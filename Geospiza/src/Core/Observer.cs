using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Comonents;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Observer
{
  private static readonly Dictionary<GH_BasicSolver, Observer> _instances = new();
  public int CurrentGeneration { get; private set; }
  public Population CurrentPopulation { get; private set; }
  public List<double> AverageFitness { get; private set; } = new();
  public List<double> BestFitness { get; private set; } = new();
  public List<double> WorstFitness { get; private set; } = new();
  public List<double> TotalFitness { get; private set; } = new();
  public List<int> NumberOfUniqueIndividuals { get; private set; } = new();
  public List<int> Diversity { get; private set; }
  public List<Individual> BestIndividuals { get; private set; } = new();
  public List<double> FitnessStandardDeviation { get; private set; } = new();

  public static Observer GetInstance(GH_BasicSolver solver)
  {
    if (!_instances.ContainsKey(solver)) _instances[solver] = new Observer();
    return _instances[solver];
  }

  /// <summary>
  ///   Creates a snapshot of the current population
  /// </summary>
  /// <param name="currentPopulation"></param>
  public void Snapshot(Population currentPopulation)
  {
    BestFitness ??= new List<double>();
    WorstFitness ??= new List<double>();
    AverageFitness ??= new List<double>();
    TotalFitness ??= new List<double>();
    NumberOfUniqueIndividuals ??= new List<int>();
    FitnessStandardDeviation ??= new List<double>();

    BestFitness.Add(currentPopulation.Inhabitants.Max(inh => inh.Fitness));
    WorstFitness.Add(currentPopulation.Inhabitants.Min(inh => inh.Fitness));
    AverageFitness.Add(currentPopulation.GetAverageFitness());
    TotalFitness.Add(currentPopulation.CalculateTotalFitness());
    NumberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());

    // Calculate standard deviation of fitness
    var average = currentPopulation.GetAverageFitness();
    var sumOfSquaresOfDifferences = currentPopulation.Inhabitants
      .Select(val => (val.Fitness - average) * (val.Fitness - average)).Sum();
    var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / currentPopulation.Inhabitants.Count);

    FitnessStandardDeviation.Add(standardDeviation);
  }

  public void SetPopulation(Population population)
  {
    CurrentPopulation = population;
    var bestIndividual = CurrentPopulation.SelectTopIndividuals(1)[0];
    BestIndividuals.Add(bestIndividual);
  }

  /// <summary>
  ///   Sets the new generation int
  /// </summary>
  public void UpdateGenerationCounter()
  {
    CurrentGeneration++;
  }

  /// <summary>
  ///   Reset the observer
  /// </summary>
  public void Reset()
  {
    AverageFitness = new List<double>();
    CurrentPopulation = null;
    CurrentGeneration = 0;
    BestIndividuals = new List<Individual>();
    BestFitness = new List<double>();
    WorstFitness = new List<double>();
    TotalFitness = new List<double>();
    NumberOfUniqueIndividuals = new List<int>();
    Diversity = new List<int>();
  }

  /// <summary>
  ///   Gets the current population
  /// </summary>
  /// <returns></returns>
  public Population GetCurrentPopulation()
  {
    return CurrentPopulation;
  }

  public string ToJson()
  {
    return JsonConvert.SerializeObject(this, Formatting.Indented);
  }
}