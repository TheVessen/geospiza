using System.Collections.Concurrent;
using GeospizaCore.ParallelSpiza;
using GeospizaCore.Utils;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GeospizaCore.Core;

/// <summary>
///     Observes and tracks the evolution of a population across generations.
///     Implements the Singleton pattern per GH_Component and provides thread-safe access to evolution metrics.
/// </summary>
/// <remarks>
///     This class maintains statistics about population fitness, diversity, and best individuals
///     across generations. It supports JSON serialization for data persistence and transfer.
///     Use <see cref="ObserverServerSnapshot" /> for a simplified implementation
/// </remarks>
public class EvolutionObserver
{
    // Define a delegate for the event
    public delegate void GenerationCompletedEventHandler(object sender, GenerationCompletedEventArgs e);

    /// <summary>
    ///     Thread-safe dictionary storing observer instances per Grasshopper component
    /// </summary>
    private static readonly ConcurrentDictionary<GH_Component, EvolutionObserver> _instances = new();

    private readonly List<double> _averageFitness = new();
    private readonly List<double> _bestFitness = new();
    private readonly List<Individual> _bestIndividuals = new();
    private readonly List<int> _diversity = new();
    private readonly List<double> _fitnessStandardDeviation = new();
    private readonly List<IReadOnlyList<Individual>> _paretoFronts = new();

    /// <summary>
    ///     Lock object for thread-safe access to internal lists
    /// </summary>
    private readonly object _listLock = new();

    private readonly List<int> _numberOfUniqueIndividuals = new();
    private readonly List<double> _totalFitness = new();
    private readonly List<double> _worstFitness = new();
    private bool _isDisposed;

    private EvolutionObserver()
    {
    }

    /// <summary>
    ///     Marks this observer as disposed and removes it from the instance cache.
    /// </summary>
    public void Dispose()
    {
        lock (_listLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _totalFitness.Clear();
            _numberOfUniqueIndividuals.Clear();
            _diversity.Clear();
            _bestIndividuals.Clear();
            _fitnessStandardDeviation.Clear();
            _paretoFronts.Clear();
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
        }
    }

    /// <summary>
    ///     Removes the observer instance associated with the given solver component from the cache.
    /// </summary>
    public static void RemoveInstance(GH_Component solver)
    {
        if (_instances.TryRemove(solver, out var observer))
            observer.Dispose();
    }

    /// <summary>
    ///     Gets the current generation number being observed
    /// </summary>
    public int CurrentGenerationIndex { get; private set; }

    /// <summary>
    ///     Gets the current population under observation
    /// </summary>
    public Population CurrentPopulation { get; private set; }

    /// <summary>
    ///     Gets the average fitness of the population across generations
    /// </summary>
    public IReadOnlyList<double> AverageFitness => _averageFitness;

    /// <summary>
    ///     Gets the best fitness of the population across generations
    /// </summary>
    public IReadOnlyList<double> BestFitness => _bestFitness;

    /// <summary>
    ///     Gets the worst fitness of the population across generations
    /// </summary>
    public IReadOnlyList<double> WorstFitness => _worstFitness;

    /// <summary>
    ///     Gets the total fitness of the population across generations
    /// </summary>
    public IReadOnlyList<double> TotalFitness => _totalFitness;

    /// <summary>
    ///     Gets the number of unique individuals in the population across generations
    /// </summary>
    public IReadOnlyList<int> NumberOfUniqueIndividuals => _numberOfUniqueIndividuals;

    /// <summary>
    ///     Gets the diversity of the population across generations
    /// </summary>
    public IReadOnlyList<int> Diversity => _diversity;

    /// <summary>
    ///     Gets the best individuals of the population across generations
    /// </summary>
    public IReadOnlyList<Individual> BestIndividuals => _bestIndividuals;

    /// <summary>
    ///     Gets the standard deviation of fitness values across generations
    /// </summary>
    public IReadOnlyList<double> FitnessStandardDeviation => _fitnessStandardDeviation;

    /// <summary>
    ///     Gets the Pareto-optimal front (rank 0) recorded per generation.
    ///     Empty lists are stored for generations using single-objective fitness.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Individual>> ParetoFronts => _paretoFronts;

    public event GenerationCompletedEventHandler GenerationCompleted;

    /// <summary>
    ///     Creates or retrieves an EvolutionObserver instance for a specific Grasshopper component
    /// </summary>
    /// <param name="solver">The Grasshopper component requiring observation</param>
    /// <returns>An EvolutionObserver instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when solver is null</exception>
    public static EvolutionObserver GetInstance(GH_Component solver)
    {
        if (_instances.TryGetValue(solver, out var observer) && observer._isDisposed)
            _instances.TryRemove(solver, out _);
        return _instances.GetOrAdd(solver, _ => new EvolutionObserver());
    }

    /// <summary>
    ///     Takes a snapshot of the current population's statistics
    /// </summary>
    /// <param name="currentPopulation">The population to analyze</param>
    public void Snapshot(Population currentPopulation)
    {
        lock (_listLock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));

            var inhabitants = currentPopulation.Inhabitants;
            var n = inhabitants.Count;

            // Single pass: min, max, and sum
            var best = double.MinValue;
            var worst = double.MaxValue;
            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                var f = inhabitants[i].Fitness;
                if (f > best) best = f;
                if (f < worst) worst = f;
                sum += f;
            }
            var average = sum / n;

            // Second pass: variance for standard deviation
            var sumOfSquares = 0.0;
            for (var i = 0; i < n; i++)
            {
                var diff = inhabitants[i].Fitness - average;
                sumOfSquares += diff * diff;
            }

            _bestFitness.Add(best);
            _worstFitness.Add(worst);
            _totalFitness.Add(sum);
            _averageFitness.Add(average);
            _fitnessStandardDeviation.Add(Math.Sqrt(sumOfSquares / n));
            _numberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());

            // Pareto front tracking — only when individuals carry multi-objective data.
            // Read ParetoRank already assigned by the solver (no re-sort here, which would overwrite
            // the combined-pool ranks used for next-generation tournament selection).
            // Store copies so historical fronts are not mutated by subsequent sorts.
            if (n > 0 && inhabitants[0].Objectives != null && inhabitants[0].Objectives.Length > 0)
            {
                var front0 = inhabitants
                    .Where(ind => ind.ParetoRank == 0)
                    .Select(ind => new Individual(ind))
                    .ToList();
                _paretoFronts.Add(front0.Count > 0
                    ? (IReadOnlyList<Individual>)front0
                    : Array.Empty<Individual>());
            }
            else
            {
                _paretoFronts.Add(Array.Empty<Individual>());
            }
        }

        SetPopulation(currentPopulation);
        UpdateGenerationCounter();

        OnGenerationCompleted(new GenerationCompletedEventArgs(CurrentGenerationIndex, CurrentPopulation));
    }

    /// <summary>
    ///     Sets the current population and records its best individual
    /// </summary>
    private void SetPopulation(Population population)
    {
        lock (_listLock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));

            CurrentPopulation = population;
            if (population.Inhabitants.Count == 0)
                throw new InvalidOperationException("No individuals found in population");
            var bestIndividual = population.Inhabitants.Aggregate((a, b) => a.Fitness >= b.Fitness ? a : b);
            _bestIndividuals.Add(bestIndividual);
        }
    }

    /// <summary>
    ///     Increments the generation counter by one
    /// </summary>
    private void UpdateGenerationCounter()
    {
        CurrentGenerationIndex++;
    }

    /// <summary>
    ///     Resets the observer instance to its initial state
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Reset()
    {
        lock (_listLock)
        {
            if (_isDisposed) throw new InvalidOperationException("Cannot reset a disposed EvolutionObserver instance.");

            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _totalFitness.Clear();
            _numberOfUniqueIndividuals.Clear();
            _diversity.Clear();
            _bestIndividuals.Clear();
            _fitnessStandardDeviation.Clear();
            _paretoFronts.Clear();

            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
        }
    }

    /// <summary>
    ///     Serializes the observer instance to a JSON string
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Individual.IndividualConverter() }
        };

        return JsonConvert.SerializeObject(this, settings);
    }

    /// <summary>
    ///     Tries to deserialize a JSON string into an EvolutionObserver instance
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static EvolutionObserver? FromJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new Individual.IndividualConverter() },
            ContractResolver = new PrivateSetterContractResolver()
        };

        return JsonConvert.DeserializeObject<EvolutionObserver>(json, settings);
    }

    protected virtual void OnGenerationCompleted(GenerationCompletedEventArgs e)
    {
        GenerationCompleted?.Invoke(this, e);
    }


    public class GenerationCompletedEventArgs : EventArgs
    {
        public GenerationCompletedEventArgs(int generationIndex, Population population)
        {
            GenerationIndex = generationIndex;
            Population = population;
        }

        public int GenerationIndex { get; }
        public Population Population { get; }
    }
}