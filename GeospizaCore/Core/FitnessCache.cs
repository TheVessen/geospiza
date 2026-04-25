namespace GeospizaCore.Core;

/// <summary>
///     Per-run cache of fitness evaluations keyed on the genotype's tick-value sequence.
///     Individuals with identical gene pools (which always trigger an identical Grasshopper
///     solve under deterministic fitness functions) reuse the cached result instead of
///     re-running <c>doc.NewSolution</c>, the dominant cost of an evolutionary search.
///     <para>
///         Disable via <see cref="Solvers.SolverSettings.UseFitnessCache" /> when the fitness
///         function is stochastic (random sampling, time-of-day, external state), since
///         caching would mask the per-evaluation variance that the algorithm needs to see.
///     </para>
///     <para>
///         The cache is single-thread-safe only. The solver loop is sequential, so this is fine
///         today; if evaluation is ever parallelized, callers must add synchronization or
///         partition the cache.
///     </para>
/// </summary>
public sealed class FitnessCache
{
    private readonly Dictionary<GeneFingerprint, Entry> _cache = new();

    /// <summary>
    ///     Number of evaluations served from the cache during the current run.
    /// </summary>
    public int Hits { get; private set; }

    /// <summary>
    ///     Number of evaluations that fell through to a real Grasshopper solve during the current run.
    /// </summary>
    public int Misses { get; private set; }

    /// <summary>
    ///     Number of distinct genotypes currently stored.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    ///     Looks up a previously cached evaluation for the given gene pool.
    ///     Returns <c>true</c> on a hit and increments <see cref="Hits" />; returns <c>false</c>
    ///     on a miss and increments <see cref="Misses" />.
    /// </summary>
    public bool TryGet(IReadOnlyList<Gene> genePool, out Entry entry)
    {
        var key = Fingerprint(genePool);
        if (_cache.TryGetValue(key, out entry))
        {
            Hits++;
            return true;
        }
        Misses++;
        return false;
    }

    /// <summary>
    ///     Stores the result of a fresh evaluation. <paramref name="objectives" /> is cloned
    ///     so subsequent mutations of the source array do not corrupt the cache.
    /// </summary>
    public void Store(IReadOnlyList<Gene> genePool, double fitness, double[]? objectives)
    {
        var key = Fingerprint(genePool);
        var entry = new Entry(
            fitness,
            objectives != null ? (double[])objectives.Clone() : null);
        _cache[key] = entry;
    }

    /// <summary>
    ///     Drops every entry and resets hit/miss counters. Solvers call this at the start of
    ///     each run because the GH document or fitness wiring may have changed since last time.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        Hits = 0;
        Misses = 0;
    }

    private static GeneFingerprint Fingerprint(IReadOnlyList<Gene> genePool)
    {
        var ticks = new int[genePool.Count];
        for (var i = 0; i < genePool.Count; i++)
            ticks[i] = genePool[i].TickValue;
        return new GeneFingerprint(ticks);
    }

    /// <summary>
    ///     A cached evaluation result. <see cref="Objectives" /> is null for single-objective runs.
    /// </summary>
    public readonly struct Entry
    {
        public Entry(double fitness, double[]? objectives)
        {
            Fitness = fitness;
            Objectives = objectives;
        }

        public double Fitness { get; }
        public double[]? Objectives { get; }
    }

    /// <summary>
    ///     Value-equality wrapper over the gene-pool tick sequence. The genotype layout is
    ///     stable within a run, so the tick-value vector alone uniquely identifies a genotype.
    /// </summary>
    private readonly struct GeneFingerprint : IEquatable<GeneFingerprint>
    {
        private readonly int[] _ticks;
        private readonly int _hash;

        public GeneFingerprint(int[] ticks)
        {
            _ticks = ticks;
            unchecked
            {
                var h = 17;
                for (var i = 0; i < ticks.Length; i++)
                    h = h * 31 + ticks[i];
                _hash = h;
            }
        }

        public bool Equals(GeneFingerprint other)
        {
            if (_ticks == null || other._ticks == null) return _ticks == other._ticks;
            if (_hash != other._hash) return false;
            if (_ticks.Length != other._ticks.Length) return false;
            for (var i = 0; i < _ticks.Length; i++)
                if (_ticks[i] != other._ticks[i]) return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is GeneFingerprint other && Equals(other);

        public override int GetHashCode() => _hash;
    }
}
