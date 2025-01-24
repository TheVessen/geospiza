using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;

namespace GeospizaCore.Solvers;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
  protected readonly Random Random = new();
  protected Population Population { get; set; } = new();
  protected int PopulationSize { get; set; }
  /// <summary>
  /// Maximum of generations that should be run. The algorithm will stop after this number of generations or a termination condition is met.
  /// </summary>
  protected int MaxGenerations { get; set; }
  /// <summary>
  /// The number of the best individuals that should be preserved for the next generation.
  /// </summary>
  protected int EliteSize { get; set; }
  /// <summary>
  /// Selection strategy that should be used for the evolutionary algorithm.
  /// </summary>
  protected ISelectionStrategy SelectionStrategy { get; set; }
  /// <summary>
  /// Crossover strategy that should be used for the evolutionary algorithm.
  /// </summary>
  protected ICrossoverStrategy CrossoverStrategy { get; set; }
  /// <summary>
  /// Mutation strategy that should be used for the evolutionary algorithm.
  /// </summary>
  protected IMutationStrategy MutationStrategy { get; set; }
  /// <summary>
  /// Pairing strategy that should be used for the evolutionary algorithm.
  /// </summary>
  protected PairingStrategy PairingStrategy { get; set; }
  /// <summary>
  /// Termination strategy that should be used for the evolutionary algorithm.
  /// </summary>
  protected ITerminationStrategy TerminationStrategy { get; set; }

  /// <summary>
  ///   Initializes the evolutionary algorithm with the given settings.
  /// </summary>
  /// <param name="settings"></param>
  protected EvolutionBlueprint(SolverSettings settings)
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

  /// <summary>
  ///   Main method to run the evolutionary algorithm.
  /// </summary>
  public abstract void RunAlgorithm();

  /// <summary>
  ///   Initializes the first population for the evolutionary algorithm.
  /// </summary>
  public void InitializePopulation(StateManager stateManager, EvolutionObserver evolutionObserver)
  {
    var fistGenBestFitness = 0.0;
    var fitnessInstance = Fitness.Instance;

    var newPopulation = new Population();
    for (var i = 0; i < PopulationSize; i++)
    {
      var individual = new Individual();

      foreach (var geneTemplate in stateManager.Genotype)
      {
        var ctg = geneTemplate.Value;
        ctg.SetTickValue(Random.Next(ctg.TickCount), stateManager);

        var stableGene = new Gene(ctg.TickValue, ctg.GeneGuid,
          ctg.TickCount, ctg.Name, ctg.GhInstanceGuid,
          ctg.GenePoolIndex);

        individual.AddGene(stableGene);
      }

      if (stateManager.PreviewLevel == 0)
        stateManager.GetDocument().NewSolution(false);
      else
        stateManager.GetDocument().NewSolution(false, GH_SolutionMode.Silent);

      var currentFitness = fitnessInstance.GetFitness();

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
      newPopulation.AddIndividual(individual);
    }

    evolutionObserver.Snapshot(newPopulation);
    Population = newPopulation;
    if (stateManager.PreviewLevel == 1) stateManager.GetDocument().ExpirePreview(true);
  }
}