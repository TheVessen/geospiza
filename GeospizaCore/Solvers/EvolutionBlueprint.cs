using GeospizaManager.Core;
using GeospizaManager.Strategies;
using Grasshopper.Kernel;

namespace GeospizaManager.Solvers;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
  protected readonly Random Random = new();
  protected Population Population { get; set; } = new();
  protected int PopulationSize { get; set; }
  protected int MaxGenerations { get; set; }
  protected int EliteSize { get; set; }
  protected ISelectionStrategy SelectionStrategy { get; set; }
  protected ICrossoverStrategy CrossoverStrategy { get; set; }
  protected IMutationStrategy MutationStrategy { get; set; }
  protected PairingStrategy PairingStrategy { get; set; }
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
  ///   Initializes the population for the evolutionary algorithm.
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

        individual.AddStableGene(stableGene);
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