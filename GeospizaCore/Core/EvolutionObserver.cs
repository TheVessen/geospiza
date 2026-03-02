using System.Collections.Concurrent;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GeospizaCore.Core;

/// <summary>
///     Observes and tracks the evolution of a population across generations.
///     Implements the Singleton pattern per GH_Component and provides thread-safe access to evolution metrics.
/// </summary>
/// <remarks>
///     Generation history is stored in compact form: gene metadata (<see cref="GeneSchema" />) is recorded once
///     and per-generation data contains only tick values and fitness, avoiding massive metadata repetition.
/// </remarks>
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
        Converters = { new Individual.IndividualConverter() }
    };

    private static readonly JsonSerializerSettings _fromJsonSettings = new()
    {
        Converters = { new Individual.IndividualConverter() }
    };

    // Compact generation history: tick values + fitness only (gene metadata lives in _geneSchema).
    private readonly List<IReadOnlyList<IndividualSnapshot>> _allGenerations = new();
    private readonly List<double> _averageFitness = new();
    private readonly List<double> _bestFitness = new();
    private readonly List<Individual> _bestIndividuals = new();
    private readonly List<int> _diversity = new();
    private readonly List<double> _fitnessStandardDeviation = new();

    private readonly List<int> _frontCount = new();

    // Hypervolume indicator per generation. 0 for single-objective runs.
    private readonly List<double> _hypervolume = new();
    private readonly object _listLock = new();

    private readonly List<int> _numberOfUniqueIndividuals = new();

    // Per generation, per objective: [min, max, mean]. Empty array for single-objective runs.
    private readonly List<double[][]> _objectiveStats = new();

    // Number of rank-0 individuals per generation. Replaces the old _paretoFronts deep-copy list.
    private readonly List<int> _paretoFrontSizes = new();
    private readonly List<double> _totalFitness = new();

    private readonly List<double> _worstFitness = new();

    // Shared gene metadata — extracted once from the first generation (genes don't change during a run).

    // Fixed HV reference point — computed once from the initial population's nadir and never changed.
    // Freezing it ensures HV values are comparable across all generations.
    private double[]? _hvReferencePoint;

    private bool _isDisposed;

    // Objective names — extracted once from the Fitness singleton on the first multi-objective snapshot.

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

    public IReadOnlyList<double> AverageFitness => _averageFitness;
    public IReadOnlyList<double> BestFitness => _bestFitness;
    public IReadOnlyList<double> WorstFitness => _worstFitness;
    public IReadOnlyList<double> TotalFitness => _totalFitness;
    public IReadOnlyList<int> NumberOfUniqueIndividuals => _numberOfUniqueIndividuals;
    public IReadOnlyList<int> Diversity => _diversity;
    public IReadOnlyList<Individual> BestIndividuals => _bestIndividuals;
    public IReadOnlyList<double> FitnessStandardDeviation => _fitnessStandardDeviation;
    public IReadOnlyList<int> FrontCount => _frontCount;
    public IReadOnlyList<double[][]> ObjectiveStats => _objectiveStats;

    /// <summary>
    ///     Gene metadata shared by every individual across all generations.
    ///     Null until the first <see cref="Snapshot" /> call.
    /// </summary>
    public GeneSchema[]? GeneSchema { get; private set; }

    /// <summary>
    ///     Names of the objectives, taken from the Multi-Objective Fitness component's input parameter names.
    ///     Null for single-objective runs. Captured once from the Fitness singleton on the first snapshot.
    /// </summary>
    public string[]? ObjectiveNames { get; private set; }

    /// <summary>
    ///     Compact snapshots of every generation's population in order.
    ///     Each entry contains tick values and fitness data only; reconstruct full individuals with
    ///     <see cref="IndividualSnapshot.ToIndividual" />.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<IndividualSnapshot>> AllGenerations => _allGenerations;

    /// <summary>
    ///     Number of rank-0 (Pareto-optimal) individuals per generation.
    ///     0 for single-objective runs.
    /// </summary>
    public IReadOnlyList<int> ParetoFrontSizes => _paretoFrontSizes;

    /// <summary>
    ///     Hypervolume indicator per generation. Computed against a fixed reference point
    ///     derived from the initial population's nadir (set once, never updated).
    ///     This ensures values are comparable across all generations.
    ///     0 for single-objective runs.
    /// </summary>
    public IReadOnlyList<double> Hypervolume => _hypervolume;

    public event GenerationCompletedEventHandler GenerationCompleted;

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
            _allGenerations.Clear();
            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _totalFitness.Clear();
            _numberOfUniqueIndividuals.Clear();
            _diversity.Clear();
            _bestIndividuals.Clear();
            _fitnessStandardDeviation.Clear();
            _frontCount.Clear();
            _objectiveStats.Clear();
            _paretoFrontSizes.Clear();
            _hypervolume.Clear();
            _hvReferencePoint = null;
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
            Algorithm = AlgorithmType.SingleObjective;
        }
    }

    /// <summary>
    ///     Takes a compact snapshot of the current population's statistics.
    ///     Gene metadata is extracted once; subsequent generations store only tick values.
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

            // Compact snapshot: tick values + fitness data only (no Gene metadata duplication).
            var snapshot = new IndividualSnapshot[n];
            for (var i = 0; i < n; i++)
                snapshot[i] = IndividualSnapshot.FromIndividual(inhabitants[i]);
            _allGenerations.Add(snapshot);

            var isMultiObjective = inhabitants[0].Objectives is { Length: > 0 };

            // Best individual — linear O(n) scan instead of O(n log n) sort.
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

            _bestIndividuals.Add(bestIndividual);

            CurrentPopulation = currentPopulation;

            // Capture objective names from the Fitness singleton each generation so
            // that user renames on GH_MultiObjectiveFitness are always reflected.
            if (isMultiObjective)
            {
                var names = Fitness.Instance.GetObjectiveNames();
                ObjectiveNames = names.Length > 0 ? (string[])names.Clone() : null;
            }

            // Multi-objective tracking — single pass for objectives, rank-0 count, and max rank.
            if (isMultiObjective)
            {
                var objCount = inhabitants[0].Objectives!.Length;
                var mins = new double[objCount];
                var maxs = new double[objCount];
                var sums = new double[objCount];
                for (var m = 0; m < objCount; m++)
                {
                    mins[m] = double.MaxValue;
                    maxs[m] = double.MinValue;
                }

                var rank0Count = 0;
                var maxRank = 0;
                for (var i = 0; i < n; i++)
                {
                    var obj = inhabitants[i].Objectives!;
                    for (var m = 0; m < objCount; m++)
                    {
                        if (obj[m] < mins[m]) mins[m] = obj[m];
                        if (obj[m] > maxs[m]) maxs[m] = obj[m];
                        sums[m] += obj[m];
                    }

                    if (inhabitants[i].ParetoRank == 0) rank0Count++;
                    if (inhabitants[i].ParetoRank > maxRank) maxRank = inhabitants[i].ParetoRank;
                }

                var stats = new double[objCount][];
                for (var m = 0; m < objCount; m++)
                    stats[m] = new[] { mins[m], maxs[m], sums[m] / n };
                _objectiveStats.Add(stats);
                _frontCount.Add(maxRank + 1);
                _paretoFrontSizes.Add(rank0Count);

                // Fix reference point once from the initial population — never updated after that.
                // Using a stable reference point ensures HV is comparable across generations.
                if (_hvReferencePoint == null)
                {
                    _hvReferencePoint = new double[objCount];
                    for (var m = 0; m < objCount; m++)
                    {
                        var range = maxs[m] - mins[m];
                        _hvReferencePoint[m] = mins[m] - Math.Max(range * 0.1, 1e-4);
                    }
                }

                var refPoint = _hvReferencePoint;

                var rank0Front = new List<Individual>(rank0Count);
                for (var i = 0; i < n; i++)
                    if (inhabitants[i].ParetoRank == 0)
                        rank0Front.Add(inhabitants[i]);

                _hypervolume.Add(HypervolumeUtils.Compute(rank0Front, refPoint));
            }
            else
            {
                _objectiveStats.Add(Array.Empty<double[]>());
                _frontCount.Add(0);
                _paretoFrontSizes.Add(0);
                _hypervolume.Add(0.0);
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
            _allGenerations.Clear();
            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _totalFitness.Clear();
            _numberOfUniqueIndividuals.Clear();
            _diversity.Clear();
            _bestIndividuals.Clear();
            _fitnessStandardDeviation.Clear();
            _frontCount.Clear();
            _objectiveStats.Clear();
            _paretoFrontSizes.Clear();
            _hypervolume.Clear();
            _hvReferencePoint = null;
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
            Algorithm = AlgorithmType.SingleObjective;
        }
    }

    /// <summary>
    ///     Serializes the observer to a compact JSON string.
    ///     Gene metadata is stored once; per-generation data contains only tick values and fitness.
    /// </summary>
    public string ToJson()
    {
        lock (_listLock)
        {
            var dto = new
            {
                CurrentGenerationIndex,
                Algorithm,
                GeneSchema = GeneSchema ?? Array.Empty<GeneSchema>(),
                ObjectiveNames = ObjectiveNames ?? [],
                BestFitness = _bestFitness,
                AverageFitness = _averageFitness,
                WorstFitness = _worstFitness,
                TotalFitness = _totalFitness,
                NumberOfUniqueIndividuals = _numberOfUniqueIndividuals,
                Diversity = _diversity,
                FitnessStandardDeviation = _fitnessStandardDeviation,
                FrontCount = _frontCount,
                ParetoFrontSizes = _paretoFrontSizes,
                Hypervolume = _hypervolume,
                ObjectiveStats = _objectiveStats,
                BestIndividuals = _bestIndividuals,
                AllGenerations = _allGenerations
            };
            return JsonConvert.SerializeObject(dto, _toJsonSettings);
        }
    }

    /// <summary>
    ///     Reconstructs an <see cref="EvolutionObserver" /> from a JSON string produced by <see cref="ToJson" />.
    ///     <see cref="CurrentPopulation" /> is rebuilt from the last recorded generation.
    /// </summary>
    public static EvolutionObserver? FromJson(string json)
    {
        var dto = JsonConvert.DeserializeObject<ObserverDto>(json, _fromJsonSettings);
        if (dto == null) return null;

        var obs = new EvolutionObserver();
        obs.GeneSchema = dto.GeneSchema;
        obs.ObjectiveNames = dto.ObjectiveNames is { Length: > 0 } ? dto.ObjectiveNames : null;
        obs._bestFitness.AddRange(dto.BestFitness);
        obs._averageFitness.AddRange(dto.AverageFitness);
        obs._worstFitness.AddRange(dto.WorstFitness);
        obs._totalFitness.AddRange(dto.TotalFitness);
        obs._numberOfUniqueIndividuals.AddRange(dto.NumberOfUniqueIndividuals);
        obs._diversity.AddRange(dto.Diversity);
        obs._bestIndividuals.AddRange(dto.BestIndividuals);
        obs._fitnessStandardDeviation.AddRange(dto.FitnessStandardDeviation);
        obs._frontCount.AddRange(dto.FrontCount);
        obs._objectiveStats.AddRange(dto.ObjectiveStats);
        obs._paretoFrontSizes.AddRange(dto.ParetoFrontSizes);
        obs._hypervolume.AddRange(dto.Hypervolume);

        foreach (var gen in dto.AllGenerations)
            obs._allGenerations.Add(gen);

        // Rebuild CurrentPopulation from the last recorded generation.
        if (dto.GeneSchema is { Length: > 0 } && dto.AllGenerations.Count > 0)
        {
            var pop = new Population();
            foreach (var snap in dto.AllGenerations[dto.AllGenerations.Count - 1])
                pop.AddIndividual(snap.ToIndividual(dto.GeneSchema));
            obs.CurrentPopulation = pop;
        }

        obs.CurrentGenerationIndex = dto.CurrentGenerationIndex;
        obs.Algorithm = dto.Algorithm;
        return obs;
    }

    protected virtual void OnGenerationCompleted(GenerationCompletedEventArgs e)
    {
        GenerationCompleted?.Invoke(this, e);
    }

    private class ObserverDto
    {
        public int CurrentGenerationIndex { get; set; }
        public AlgorithmType Algorithm { get; set; } = AlgorithmType.SingleObjective;
        public GeneSchema[]? GeneSchema { get; set; }
        public string[]? ObjectiveNames { get; set; }
        public List<double> BestFitness { get; } = new();
        public List<double> AverageFitness { get; } = new();
        public List<double> WorstFitness { get; } = new();
        public List<double> TotalFitness { get; } = new();
        public List<int> NumberOfUniqueIndividuals { get; } = new();
        public List<int> Diversity { get; } = new();
        public List<Individual> BestIndividuals { get; } = new();
        public List<double> FitnessStandardDeviation { get; } = new();
        public List<int> FrontCount { get; } = new();
        public List<double[][]> ObjectiveStats { get; } = new();
        public List<int> ParetoFrontSizes { get; } = new();
        public List<double> Hypervolume { get; } = new();
        public List<List<IndividualSnapshot>> AllGenerations { get; } = new();
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