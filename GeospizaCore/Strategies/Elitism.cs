using GeospizaManager.Core;

namespace GeospizaManager.Strategies;

public static class Elitism
{
  /// <summary>
  ///   Selects the top individuals from the population.
  /// </summary>
  /// <param name="eliteSize"></param>
  /// <returns></returns>
  public static List<Individual> SelectTopIndividuals(int eliteSize, List<Individual> inhabitants)
  {
    var copiedPopulation = inhabitants.Select(individual => new Individual(individual)).ToList();
    copiedPopulation.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
    return copiedPopulation.Take(eliteSize).ToList();
  }
}