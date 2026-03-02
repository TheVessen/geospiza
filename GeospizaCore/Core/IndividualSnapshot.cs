namespace GeospizaCore.Core;

/// <summary>
///     Compact per-generation snapshot of an individual.
///     Stores only the variable tick values plus fitness data — gene metadata lives once in
///     <see cref="EvolutionObserver.GeneSchema" /> and is not repeated per individual or generation.
/// </summary>
public class IndividualSnapshot
{
    public int[] TickValues { get; set; } = Array.Empty<int>();
    public double Fitness { get; set; }
    public double Probability { get; set; }
    public double[]? Objectives { get; set; }
    public int ParetoRank { get; set; }
    public double CrowdingDistance { get; set; }
    public int Generation { get; set; }

    /// <summary>
    ///     Reconstructs a full <see cref="Individual" /> from this snapshot and the shared gene schema.
    /// </summary>
    public Individual ToIndividual(GeneSchema[] schema)
    {
        var genes = new Gene[schema.Length];
        for (var i = 0; i < schema.Length; i++)
        {
            var s = schema[i];
            genes[i] = new Gene(TickValues[i], s.GeneGuid, s.TickCount, s.GeneName, s.GhInstanceGuid, s.GenePoolIndex);
        }

        var ind = new Individual(genes);
        ind.SetFitness(Fitness);
        ind.SetProbability(Probability);
        if (Objectives != null) ind.SetObjectives((double[])Objectives.Clone());
        ind.SetParetoRank(ParetoRank);
        ind.SetCrowdingDistance(CrowdingDistance);
        ind.SetGeneration(Generation);
        return ind;
    }

    /// <summary>
    ///     Creates a compact snapshot from a full <see cref="Individual" />.
    /// </summary>
    public static IndividualSnapshot FromIndividual(Individual individual)
    {
        var pool = individual.GenePool;
        var ticks = new int[pool.Count];
        for (var i = 0; i < pool.Count; i++)
            ticks[i] = pool[i].TickValue;

        return new IndividualSnapshot
        {
            TickValues = ticks,
            Fitness = individual.Fitness,
            Probability = individual.Probability,
            Objectives = individual.Objectives != null ? (double[])individual.Objectives.Clone() : null,
            ParetoRank = individual.ParetoRank,
            CrowdingDistance = individual.CrowdingDistance,
            Generation = individual.Generation
        };
    }
}