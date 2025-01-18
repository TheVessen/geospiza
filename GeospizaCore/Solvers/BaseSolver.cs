using GeospizaManager.Core;
using GeospizaManager.Strategies;

namespace GeospizaManager.Solvers;

/// <summary>
/// Base class for evolutionary solvers
/// </summary>
public class BaseSolver : EvolutionBlueprint
{
  private const int TerminationEvaluationThreshold = 5;

  public BaseSolver(SolverSettings settings, StateManager stateManager,
    EvolutionObserver evolutionObserver) :
    base(settings)
  {
    StateManager = stateManager;
    EvolutionObserver = evolutionObserver;
  }

  private StateManager StateManager { get; }
  private EvolutionObserver EvolutionObserver { get; }

  public override void RunAlgorithm()
  {
    // Initialize the population
    InitializePopulation(StateManager, EvolutionObserver);
    try
    {
      // Run the algorithm for the specified number of generations
      for (var i = 0; i < MaxGenerations - 1; i++)
      {
        var populationCopy = new Population(Population);
        var newPopulation = new Population();

        var elite = Elitism.SelectTopIndividuals(EliteSize, Population.Inhabitants);
        newPopulation.AddIndividuals(elite);

        while (newPopulation.Count < PopulationSize)
        {
          var matingPool = SelectionStrategy.Select(populationCopy, PopulationSize);
          var matingPairs = PairingStrategy.PairIndividuals(matingPool);

          foreach (var pair in matingPairs)
          {
            var children = PerformCrossover(pair);
            MutateChildren(children);
            newPopulation.AddIndividuals(children);
          }

          newPopulation.AddIndividuals(matingPool);
        }

        if (newPopulation.Count > PopulationSize)
        {
          newPopulation.Inhabitants.Sort((inhabitant1, inhabitant2) =>
            inhabitant1.Fitness.CompareTo(inhabitant2.Fitness));
          var removeCount = newPopulation.Count - PopulationSize;
          newPopulation.Inhabitants.RemoveRange(PopulationSize, removeCount);
        }

        foreach (var inhabitant in newPopulation.Inhabitants) inhabitant.SetGeneration(i + 1);

        // Test the fitness of the new population
        newPopulation.TestPopulation(StateManager, EvolutionObserver);

        // Record statistics for the current population
        StateManager.GetDocument().ExpirePreview(false);
        EvolutionObserver.Snapshot(newPopulation);

        //TODO: For multi processing here would be the point to send the observer to the main thread

        // If the termination condition is met, stop the algorithm
        if (i > TerminationEvaluationThreshold)
          if (TerminationStrategy.Evaluate(EvolutionObserver))
            break;

        // Replace the current population with the new population
        Population = newPopulation;
        if (StateManager.PreviewLevel == 1) StateManager.GetDocument().ExpirePreview(true);
      }

      // At the end of the algorithm, reinstate the best individual
      var best = Population.SelectTopIndividuals(1);
      best[0].Reinstate(StateManager);
    }
    catch (Exception ex)
    {
      Console.WriteLine($@"An error occurred: {ex.Message}");
    }
  }

  private List<Individual> PerformCrossover(IndividualPair individualPair)
  {
    return PerformOperation(individualPair, CrossoverStrategy.CrossoverRate, CrossoverStrategy.Crossover);
  }

  private void MutateChildren(List<Individual> children)
  {
    PerformOperation(children, MutationStrategy.MutationRate, MutationStrategy.Mutate);
  }

  private List<Individual> PerformOperation(IndividualPair individualPair, double rate,
    Func<Individual, Individual, List<Individual>> operation)
  {
    if (!(Random.NextDouble() < rate))
      return operation(individualPair.Individual1, individualPair.Individual2);
    return new List<Individual> { individualPair.Individual1, individualPair.Individual2 };
  }

  private void PerformOperation(List<Individual> individuals, double rate, Action<Individual> operation)
  {
    foreach (var individual in individuals)
      if (Random.NextDouble() < rate)
        operation(individual);
  }
}