using System.Collections.Concurrent;
using GeospizaCore.Solvers;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GeospizaCore.Core;

/// <summary>
///     Observes and tracks the evolution of a population across generations.
///     Implements the Singleton pattern per GH_Component and provides thread-safe access to evolution metrics.
/// </summary>
public class EvolutionObserver
{
    public enum AlgorithmType
    {
        SingleObjective,
        NsgaII,
        NsgaIII
    }

    public delegate void GenerationCompletedEventHandler(object sender, GenerationCompletedEventArgs e);

    private static readonly ConcurrentDictionary<GH_Component, EvolutionObserver> _instances = new();

    private static readonly JsonSerializerSettings _toJsonSettings = new()
    {
        FloatFormatHandling = FloatFormatHandling.String,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new Individual.IndividualConverter(), new Newtonsoft.Json.Converters.StringEnumConverter() }
    };

    private static readonly JsonSerializerSettings _fromJsonSettings = new()
    {
        Converters = { new Individual.IndividualConverter() }
    };

    // Per-generation aggregate stats
    private readonly List<double> _averageFitness = new();
    private readonly List<double> _bestFitness = new();
    private readonly List<double> _worstFitness = new();
    private readonly List<double> _fitnessStandardDeviation = new();
    private readonly List<int> _numberOfUniqueIndividuals = new();

    // Multi-objective per-generation stats
    private readonly List<double> _hypervolume = new();
    private readonly List<int> _paretoFrontSizes = new();

    private readonly object _listLock = new();

    // Fixed HV reference point — computed once from the initial population's nadir.
    private double[]? _hvReferencePoint;

    // Only the last generation's population is stored for individual reinstatement.
    private IndividualSnapshot[]? _finalPopulationSnapshot;

    private bool _isDisposed;

    private EvolutionObserver()
    {
    }

    public int CurrentGenerationIndex { get; private set; }
    public Population CurrentPopulation { get; private set; }

    /// <summary>
    ///     The algorithm type that produced this observation run.
    ///     Set by the solver before it starts running.
    /// </summary>
    public AlgorithmType Algorithm { get; private set; } = AlgorithmType.SingleObjective;

    /// <summary>
    ///     Records which algorithm is driving this observer.
    ///     Should be called once before <see cref="Snapshot" /> is first invoked.
    /// </summary>
    public void SetAlgorithmType(AlgorithmType type) => Algorithm = type;

    /// <summary>
    ///     The solver configuration that produced this observation run.
    ///     Set by the solver before it starts running.
    /// </summary>
    public SolverSettings? Settings { get; private set; }

    /// <summary>
    ///     Records the solver configuration for this run.
    ///     Should be called once before <see cref="Snapshot" /> is first invoked.
    /// </summary>
    public void SetSettings(SolverSettings settings) => Settings = settings;

    /// <summary>
    ///     Whether the termination strategy fired before MaxGenerations was reached.
    /// </summary>
    public bool TerminatedEarly { get; private set; }

    /// <summary>
    ///     Marks the run as having terminated early due to the termination strategy.
    /// </summary>
    public void SetTerminatedEarly() => TerminatedEarly = true;

    public IReadOnlyList<double> AverageFitness => _averageFitness;
    public IReadOnlyList<double> BestFitness => _bestFitness;
    public IReadOnlyList<double> WorstFitness => _worstFitness;
    public IReadOnlyList<double> FitnessStandardDeviation => _fitnessStandardDeviation;
    public IReadOnlyList<int> NumberOfUniqueIndividuals => _numberOfUniqueIndividuals;
    public IReadOnlyList<double> Hypervolume => _hypervolume;
    public IReadOnlyList<int> ParetoFrontSizes => _paretoFrontSizes;

    /// <summary>
    ///     Gene metadata shared by every individual across all generations.
    ///     Null until the first <see cref="Snapshot" /> call.
    /// </summary>
    public GeneSchema[]? GeneSchema { get; private set; }

    /// <summary>
    ///     Names of the objectives. Null for single-objective runs.
    /// </summary>
    public string[]? ObjectiveNames { get; private set; }

    /// <summary>
    ///     The best individual from the final generation.
    /// </summary>
    public Individual? FinalBestIndividual { get; private set; }

    public delegate void RunCompletedEventHandler(object sender, EventArgs e);

    public event GenerationCompletedEventHandler GenerationCompleted;
    public event RunCompletedEventHandler RunCompleted;

    public static EvolutionObserver GetInstance(GH_Component solver)
    {
        if (_instances.TryGetValue(solver, out var observer) && observer._isDisposed)
            _instances.TryRemove(solver, out _);
        return _instances.GetOrAdd(solver, _ => new EvolutionObserver());
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
    ///     Marks this observer as disposed and clears all stored data.
    /// </summary>
    public void Dispose()
    {
        lock (_listLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            GeneSchema = null;
            ObjectiveNames = null;
            Settings = null;
            FinalBestIndividual = null;
            _finalPopulationSnapshot = null;
            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _fitnessStandardDeviation.Clear();
            _numberOfUniqueIndividuals.Clear();
            _hypervolume.Clear();
            _paretoFrontSizes.Clear();
            _hvReferencePoint = null;
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
            TerminatedEarly = false;
            Algorithm = AlgorithmType.SingleObjective;
        }
    }

    /// <summary>
    ///     Takes a snapshot of the current population's aggregate statistics.
    ///     Only the last generation's full population is retained for individual reinstatement.
    /// </summary>
    public void Snapshot(Population currentPopulation)
    {
        lock (_listLock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));

            var inhabitants = currentPopulation.Inhabitants;
            var n = inhabitants.Count;
            if (n == 0) throw new InvalidOperationException("No individuals found in population");

            // Single pass: min, max, sum
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

            // Second pass: standard deviation
            var sumOfSquares = 0.0;
            for (var i = 0; i < n; i++)
            {
                var diff = inhabitants[i].Fitness - average;
                sumOfSquares += diff * diff;
            }

            _bestFitness.Add(best);
            _worstFitness.Add(worst);
            _averageFitness.Add(average);
            _fitnessStandardDeviation.Add(Math.Sqrt(sumOfSquares / n));
            _numberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());

            // Extract gene schema once — genes are identical across all generations.
            if (GeneSchema == null && inhabitants[0].GenePool.Count > 0)
            {
                var pool = inhabitants[0].GenePool;
                GeneSchema = new GeneSchema[pool.Count];
                for (var i = 0; i < pool.Count; i++)
                {
                    var g = pool[i];
                    GeneSchema[i] = new GeneSchema(g.GeneGuid, g.TickCount, g.GeneName, g.GhInstanceGuid,
                        g.GenePoolIndex);
                }
            }

            var isMultiObjective = inhabitants[0].Objectives is { Length: > 0 };

            // Best individual — linear O(n) scan.
            var bestIndividual = inhabitants[0];
            if (isMultiObjective)
            {
                var bestDist = double.MinValue;
                for (var i = 0; i < n; i++)
                {
                    var ind = inhabitants[i];
                    if (ind.ParetoRank == 0 && ind.CrowdingDistance > bestDist)
                    {
                        bestIndividual = ind;
                        bestDist = ind.CrowdingDistance;
                    }
                }
            }
            else
            {
                for (var i = 1; i < n; i++)
                    if (inhabitants[i].Fitness > bestIndividual.Fitness)
                        bestIndividual = inhabitants[i];
            }

            FinalBestIndividual = bestIndividual;

            // Replace the stored snapshot with the current generation — only the last one is kept.
            var snapshot = new IndividualSnapshot[n];
            for (var i = 0; i < n; i++)
                snapshot[i] = IndividualSnapshot.FromIndividual(inhabitants[i]);
            _finalPopulationSnapshot = snapshot;

            CurrentPopulation = currentPopulation;

            // Capture objective names each generation so renames in GH are always reflected.
            if (isMultiObjective)
            {
                var names = Fitness.Instance.GetObjectiveNames();
                ObjectiveNames = names.Length > 0 ? (string[])names.Clone() : null;
            }

            // Multi-objective tracking.
            if (isMultiObjective)
            {
                var objCount = inhabitants[0].Objectives!.Length;
                var mins = new double[objCount];
                var maxs = new double[objCount];
                for (var m = 0; m < objCount; m++)
                {
                    mins[m] = double.MaxValue;
                    maxs[m] = double.MinValue;
                }

                var rank0Count = 0;
                for (var i = 0; i < n; i++)
                {
                    var obj = inhabitants[i].Objectives!;
                    for (var m = 0; m < objCount; m++)
                    {
                        if (obj[m] < mins[m]) mins[m] = obj[m];
                        if (obj[m] > maxs[m]) maxs[m] = obj[m];
                    }
                    if (inhabitants[i].ParetoRank == 0) rank0Count++;
                }

                _paretoFrontSizes.Add(rank0Count);

                if (_hvReferencePoint == null)
                {
                    _hvReferencePoint = new double[objCount];
                    for (var m = 0; m < objCount; m++)
                    {
                        var range = maxs[m] - mins[m];
                        _hvReferencePoint[m] = mins[m] - Math.Max(range * 0.1, 1e-4);
                    }
                }

                var rank0Front = new List<Individual>(rank0Count);
                for (var i = 0; i < n; i++)
                    if (inhabitants[i].ParetoRank == 0)
                        rank0Front.Add(inhabitants[i]);

                _hypervolume.Add(HypervolumeUtils.Compute(rank0Front, _hvReferencePoint));
            }
        }

        CurrentGenerationIndex++;
        OnGenerationCompleted(new GenerationCompletedEventArgs(CurrentGenerationIndex, currentPopulation));
    }

    /// <summary>
    ///     Resets the observer instance to its initial state.
    /// </summary>
    public void Reset()
    {
        lock (_listLock)
        {
            if (_isDisposed) throw new InvalidOperationException("Cannot reset a disposed EvolutionObserver instance.");

            GeneSchema = null;
            ObjectiveNames = null;
            Settings = null;
            FinalBestIndividual = null;
            _finalPopulationSnapshot = null;
            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _fitnessStandardDeviation.Clear();
            _numberOfUniqueIndividuals.Clear();
            _hypervolume.Clear();
            _paretoFrontSizes.Clear();
            _hvReferencePoint = null;
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
            TerminatedEarly = false;
            Algorithm = AlgorithmType.SingleObjective;
        }
    }

    /// <summary>
    ///     Serializes the observer to a compact JSON string for analytics and AI analysis.
    ///     Contains per-generation aggregate stats and the final population for reinstatement.
    /// </summary>
    public string ToJson()
    {
        lock (_listLock)
        {
            var isMultiObjective = Algorithm != AlgorithmType.SingleObjective;
            var dto = new
            {
                Algorithm,
                TerminatedEarly,
                CurrentGenerationIndex,
                Settings,
                GeneSchema = GeneSchema ?? Array.Empty<GeneSchema>(),
                GeneSummary = ComputeGeneSummary(),
                ObjectiveNames = isMultiObjective ? ObjectiveNames : null,
                BestFitness = _bestFitness,
                AverageFitness = _averageFitness,
                WorstFitness = _worstFitness,
                FitnessStandardDeviation = _fitnessStandardDeviation,
                NumberOfUniqueIndividuals = _numberOfUniqueIndividuals,
                ParetoFrontSizes = isMultiObjective ? _paretoFrontSizes : null,
                Hypervolume = isMultiObjective ? _hypervolume : null,
                FinalBestIndividual,
                FinalPopulation = _finalPopulationSnapshot
            };
            return JsonConvert.SerializeObject(dto, _toJsonSettings);
        }
    }

    private object[]? ComputeGeneSummary()
    {
        if (GeneSchema == null || _finalPopulationSnapshot == null || _finalPopulationSnapshot.Length == 0)
            return null;

        var pop = _finalPopulationSnapshot;
        var n = pop.Length;
        var geneCount = GeneSchema.Length;

        // Collect fitness values once
        var fitness = new double[n];
        for (var i = 0; i < n; i++) fitness[i] = pop[i].Fitness;

        var meanFitness = fitness.Average();
        var fitnessDev = fitness.Select(f => f - meanFitness).ToArray();
        var fitnessSumSq = fitnessDev.Sum(d => d * d);

        var summary = new object[geneCount];
        for (var g = 0; g < geneCount; g++)
        {
            var ticks = new double[n];
            for (var i = 0; i < n; i++) ticks[i] = pop[i].TickValues[g];

            var mean = ticks.Average();
            var variance = ticks.Sum(t => (t - mean) * (t - mean)) / n;
            var std = Math.Sqrt(variance);
            var min = ticks.Min();
            var max = ticks.Max();

            // Pearson correlation between tick values and fitness
            var tickDev = ticks.Select(t => t - mean).ToArray();
            var tickSumSq = tickDev.Sum(d => d * d);
            var covariance = 0.0;
            for (var i = 0; i < n; i++) covariance += tickDev[i] * fitnessDev[i];
            var denom = Math.Sqrt(tickSumSq * fitnessSumSq);
            var correlation = denom > 0 ? covariance / denom : 0.0;

            summary[g] = new
            {
                GeneSchema[g].GeneGuid,
                MeanTick = Math.Round(mean, 2),
                StdTick = Math.Round(std, 2),
                MinTick = (int)min,
                MaxTick = (int)max,
                FitnessCorrelation = Math.Round(correlation, 3)
            };
        }

        return summary;
    }

    /// <summary>
    ///     Reconstructs an <see cref="EvolutionObserver" /> from a JSON string produced by <see cref="ToJson" />.
    ///     <see cref="CurrentPopulation" /> is rebuilt from the stored final population snapshot.
    /// </summary>
    public static EvolutionObserver? FromJson(string json)
    {
        var dto = JsonConvert.DeserializeObject<ObserverDto>(json, _fromJsonSettings);
        if (dto == null) return null;

        var obs = new EvolutionObserver();
        obs.GeneSchema = dto.GeneSchema;
        obs.ObjectiveNames = dto.ObjectiveNames is { Length: > 0 } ? dto.ObjectiveNames : null;
        obs.Settings = dto.Settings;
        obs.Algorithm = dto.Algorithm;
        obs.CurrentGenerationIndex = dto.CurrentGenerationIndex;
        obs.TerminatedEarly = dto.TerminatedEarly;
        obs.FinalBestIndividual = dto.FinalBestIndividual;
        obs._bestFitness.AddRange(dto.BestFitness);
        obs._averageFitness.AddRange(dto.AverageFitness);
        obs._worstFitness.AddRange(dto.WorstFitness);
        obs._fitnessStandardDeviation.AddRange(dto.FitnessStandardDeviation);
        obs._numberOfUniqueIndividuals.AddRange(dto.NumberOfUniqueIndividuals);
        obs._paretoFrontSizes.AddRange(dto.ParetoFrontSizes);
        obs._hypervolume.AddRange(dto.Hypervolume);

        // Rebuild CurrentPopulation from the final population snapshot.
        if (dto.GeneSchema is { Length: > 0 } && dto.FinalPopulation is { Count: > 0 })
        {
            obs._finalPopulationSnapshot = dto.FinalPopulation.ToArray();
            var pop = new Population();
            foreach (var snap in dto.FinalPopulation)
                pop.AddIndividual(snap.ToIndividual(dto.GeneSchema));
            obs.CurrentPopulation = pop;
        }

        return obs;
    }

    protected virtual void OnGenerationCompleted(GenerationCompletedEventArgs e)
    {
        GenerationCompleted?.Invoke(this, e);
    }

    public void NotifyRunCompleted()
    {
        RunCompleted?.Invoke(this, EventArgs.Empty);
    }

    private class ObserverDto
    {
        public int CurrentGenerationIndex { get; set; }
        public AlgorithmType Algorithm { get; set; } = AlgorithmType.SingleObjective;
        public bool TerminatedEarly { get; set; }
        public SolverSettings? Settings { get; set; }
        public GeneSchema[]? GeneSchema { get; set; }
        public string[]? ObjectiveNames { get; set; }
        public Individual? FinalBestIndividual { get; set; }
        public List<IndividualSnapshot> FinalPopulation { get; } = [];
        public List<double> BestFitness { get; } = new();
        public List<double> AverageFitness { get; } = new();
        public List<double> WorstFitness { get; } = new();
        public List<double> FitnessStandardDeviation { get; } = new();
        public List<int> NumberOfUniqueIndividuals { get; } = new();
        public List<int> ParetoFrontSizes { get; } = new();
        public List<double> Hypervolume { get; } = new();
    }

    public class GenerationCompletedEventArgs(int generationIndex, Population population) : EventArgs
    {
        public int GenerationIndex { get; } = generationIndex;
        public Population Population { get; } = population;
    }
}
