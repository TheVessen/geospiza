using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;
using Geospiza.Strategies.Crossover;
using Geospiza.Strategies.Mutation;
using Geospiza.Strategies.Pairing;
using Geospiza.Strategies.Selection;
using Geospiza.Strategies.Termination;
using Grasshopper.Kernel;

namespace Geospiza.Algorythm;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
  // Other shared properties
  protected readonly Random Random = new();

  /// <summary>
  ///   Initializes the evolutionary algorithm with the given settings.
  /// </summary>
  /// <param name="settings"></param>
  protected EvolutionBlueprint(EvolutionaryAlgorithmSettings settings)
  {
    PopulationSize = settings.PopulationSize;
    MaxGenerations = settings.MaxGenerations;
    EliteSize = settings.EliteSize;
    SelectionStrategy = settings.SelectionStrategy;
    CrossoverStrategy = settings.CrossoverStrategy;
    MutationStrategy = settings.MutationStrategy;
    PairingStrategy = settings.PairingStrategy;
    TerminationStrategy = settings.TerminationStrategy;
  }

  //Inhabitants
  protected Population Population { get; set; } = new();

  // Parameters
  protected int PopulationSize { get; set; }
  protected int MaxGenerations { get; set; }
  protected int EliteSize { get; set; }

  // Strategies
  protected ISelectionStrategy SelectionStrategy { get; set; }
  protected ICrossoverStrategy CrossoverStrategy { get; set; }
  protected IMutationStrategy MutationStrategy { get; set; }
  protected PairingStrategy PairingStrategy { get; set; }
  protected ITerminationStrategy TerminationStrategy { get; set; }


  /// <summary>
  ///   Main method to run the evolutionary algorithm.
  /// </summary>
  public abstract void RunAlgorithm();

  /// <summary>
  ///   Initializes the population for the evolutionary algorithm.
  /// </summary>
  public void InitializePopulation(StateManager stateManager, Observer observer)
  {
    //Create an empty population
    double fistGenBestFitness = 0;

    var newPopulation = new Population();
    for (var i = 0; i < PopulationSize; i++)
    {
      //Create a new solution and individual
      var individual = new Individual();

      //Go through the gene pool and create a new individual
      foreach (var templateGene in stateManager.Genotype)
      {
        var ctg = templateGene.Value;
        ctg.SetTickValue(Random.Next(ctg.TickCount), stateManager);

        var stableGene = new Gene(ctg.TickValue, ctg.GeneGuid,
          ctg.TickCount, ctg.Name, ctg.GhInstanceGuid,
          ctg.GenePoolIndex);

        individual.AddStableGene(stableGene);
      }

      if (stateManager.PreviewLevel == 0)
        stateManager.GetDocument().NewSolution(false);
      else
        stateManager.GetDocument().NewSolution(false, GH_SolutionMode.Silent);

      //Get fitness from the state state manager and apply it to the individual
      var currentFitness = stateManager.FitnessComponent.FitnessValue;

      if (i == 0)
      {
        fistGenBestFitness = currentFitness;
      }
      else if (currentFitness > fistGenBestFitness)
      {
        fistGenBestFitness = currentFitness;
        if (stateManager.PreviewLevel == 2) stateManager.GetDocument().ExpirePreview(true);
      }

      individual.SetFitness(currentFitness);
      individual.SetGeneration(0);

      //Add the individual to the population
      newPopulation.AddIndividual(individual);
    }

    observer.Snapshot(newPopulation);
    observer.SetPopulation(newPopulation);
    Population = newPopulation;
    if (stateManager.PreviewLevel == 1) stateManager.GetDocument().ExpirePreview(true);
  }

  /// <summary>
  ///   Selects the top individuals from the population.
  /// </summary>
  /// <param name="eliteSize"></param>
  /// <returns></returns>
  protected List<Individual> SelectTopIndividuals(int eliteSize)
  {
    // Sort the population by fitness in descending order
    var copiedPopulation = Population.Inhabitants.Select(individual => new Individual(individual)).ToList();

    // Take the top 'eliteSize' individuals
    copiedPopulation.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
    return copiedPopulation.Take(eliteSize).ToList();
  }
}