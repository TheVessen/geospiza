namespace GeospizaCore.Advisor;

/// <summary>
///     Snapshot of the run state an <see cref="IRunAdvisor" /> decides from. Built by the solver after
///     each generation; immutable so it can be handed to a background task.
/// </summary>
public sealed class RunSignals
{
    public RunSignals(int generation, int maxGenerations, double progress, bool isMultiObjective,
        bool windowReady, bool isStagnating, int generationsWithoutImprovement,
        double uniqueFraction, double? genotypicDiversity, bool isDiversityLow, bool isDiversityCollapsed,
        double mutationRate, double baseMutationRate, double mutationBoost, double maxMutationBoost,
        double crossoverRate, double baseCrossoverRate, double cacheHitRate,
        IReadOnlyList<AdvisorRecord> recentDecisions)
    {
        Generation = generation;
        MaxGenerations = maxGenerations;
        Progress = progress;
        IsMultiObjective = isMultiObjective;
        WindowReady = windowReady;
        IsStagnating = isStagnating;
        GenerationsWithoutImprovement = generationsWithoutImprovement;
        UniqueFraction = uniqueFraction;
        GenotypicDiversity = genotypicDiversity;
        IsDiversityLow = isDiversityLow;
        IsDiversityCollapsed = isDiversityCollapsed;
        MutationRate = mutationRate;
        BaseMutationRate = baseMutationRate;
        MutationBoost = mutationBoost;
        MaxMutationBoost = maxMutationBoost;
        CrossoverRate = crossoverRate;
        BaseCrossoverRate = baseCrossoverRate;
        CacheHitRate = cacheHitRate;
        RecentDecisions = recentDecisions;
    }

    /// <summary>Index of the generation just completed.</summary>
    public int Generation { get; }

    public int MaxGenerations { get; }

    /// <summary>0 at generation 0, 1 at the final generation.</summary>
    public double Progress { get; }

    public bool IsMultiObjective { get; }

    /// <summary>False until enough generations exist to judge stagnation.</summary>
    public bool WindowReady { get; }

    /// <summary>
    ///     Best fitness (single-objective) or hypervolume (multi-objective) flat over the adaptation window.
    /// </summary>
    public bool IsStagnating { get; }

    public int GenerationsWithoutImprovement { get; }

    /// <summary>Share of distinct individuals in the latest generation, 0-1.</summary>
    public double UniqueFraction { get; }

    /// <summary>Mean pairwise normalized gene distance, 0-1; null before the first snapshot.</summary>
    public double? GenotypicDiversity { get; }

    /// <summary>Too few distinct individuals, or genotypic diversity below the immigrant threshold.</summary>
    public bool IsDiversityLow { get; }

    /// <summary>Genotypic diversity below the immigrant threshold.</summary>
    public bool IsDiversityCollapsed { get; }

    public double MutationRate { get; }
    public double BaseMutationRate { get; }

    /// <summary>Current multiple applied on top of the scheduled mutation rate (1 = no boost).</summary>
    public double MutationBoost { get; }

    /// <summary>Largest boost still allowed at this point of the run; shrinks to 1 by the last generation.</summary>
    public double MaxMutationBoost { get; }

    public double CrossoverRate { get; }
    public double BaseCrossoverRate { get; }

    /// <summary>Cumulative fitness-cache hit rate, 0-1; 0 when caching is off.</summary>
    public double CacheHitRate { get; }

    /// <summary>The last few advisor decisions of this run, oldest first.</summary>
    public IReadOnlyList<AdvisorRecord> RecentDecisions { get; }
}
