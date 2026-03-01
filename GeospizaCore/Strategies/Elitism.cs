using GeospizaCore.Core;

namespace GeospizaCore.Strategies;

public static class Elitism
{
    /// <summary>
    ///     Selects the top individuals from the population
    /// </summary>
    /// <param name="eliteSize"></param>
    /// <param name="inhabitants"></param>
    /// <returns></returns>
    public static List<Individual> SelectTopIndividuals(int eliteSize, List<Individual> inhabitants)
    {
        var clampedSize = Math.Min(eliteSize, inhabitants.Count);
        return inhabitants
            .OrderByDescending(ind => ind.Fitness)
            .Take(clampedSize)
            .Select(ind => new Individual(ind))
            .ToList();
    }
}