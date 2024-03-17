using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Population
{
  public Population()
  {
  }

  public Population(Population population)
  {
    Inhabitants = new List<Individual>(population.Inhabitants);
  }

  public List<Individual> Inhabitants { get; private set; } = new();
  public int Count => Inhabitants.Count;

  /// <summary>
  /// </summary>
  /// <param name="individual"></param>
  public void AddIndividual(Individual individual)
  {
    Inhabitants.Add(individual);
  }

  /// <summary>
  /// </summary>
  /// <param name="individual"></param>
  public void AddIndividuals(List<Individual> individual)
  {
    if (individual == null) throw new ArgumentNullException(nameof(individual));
    Inhabitants.AddRange(individual);
  }

  public void ReplaceIndividuals(List<Individual> individuals)
  {
    Inhabitants = individuals;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="stateManager"></param>
  /// <param name="observer"></param>
  public void TestPopulation(StateManager stateManager, Observer observer)
  {
    var doc = stateManager.GetDocument();

    var max = observer.BestFitness.Max();

    foreach (var individual in Inhabitants)
    {
      foreach (var gene in individual.GenePool)
      {
        var matchingGene = stateManager.Genotype[gene.GeneGuid];
        matchingGene?.SetTickValue(gene.TickValue, stateManager);
      }

      if (stateManager.PreviewLevel == 0)
        stateManager.GetDocument().NewSolution(false);
      else
        stateManager.GetDocument().NewSolution(false, GH_SolutionMode.Silent);

      stateManager.FitnessComponent.ExpireSolution(false);
      individual.SetFitness(stateManager.FitnessComponent.FitnessValue);

      if (stateManager.PreviewLevel != 2) continue;

      if (!(max < individual.Fitness)) continue;
      
      stateManager.GetDocument().ExpirePreview(true);
      max = individual.Fitness;
    }
  }

  /// <summary>
  /// </summary>
  /// <returns></returns>
  public double CalculateTotalFitness()
  {
    return Inhabitants.Sum(ind => ind.Fitness);
  }

  /// <summary>
  /// </summary>
  /// <returns></returns>
  public double GetAverageFitness()
  {
    return CalculateTotalFitness() / Count;
  }

  /// <summary>
  /// </summary>
  /// <returns></returns>
  public int GetDiversity()
  {
    var diversity = 0;
    var uniqueHashes = new HashSet<int>();

    foreach (var individual in Inhabitants)
      if (uniqueHashes.Add(individual.GetHashCode()))
        diversity++;

    return diversity;
  }

  /// <summary>
  /// </summary>
  /// <param name="eliteSize"></param>
  /// <returns></returns>
  public List<Individual> SelectTopIndividuals(int eliteSize)
  {
    var bestIndividuals = new List<Individual>();
    var sortedPopulation = Inhabitants.OrderByDescending(ind => ind.Fitness).ToList();
    for (var i = 0; i < eliteSize; i++) bestIndividuals.Add(sortedPopulation[i]);

    return bestIndividuals;
  }

  /// <summary>
  /// </summary>
  public void CalculateProbability()
  {
    var totalFitness = CalculateTotalFitness();
    foreach (var individual in Inhabitants) individual.SetProbability(individual.Fitness / totalFitness);
  }

  public string ToJson()
  {
    return JsonConvert.SerializeObject(this);
  }
}