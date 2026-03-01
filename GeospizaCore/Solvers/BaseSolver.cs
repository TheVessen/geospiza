using GeospizaCore.Core;
using GeospizaCore.Strategies;

namespace GeospizaCore.Solvers;

/// <summary>
///     Base class for evolutionary solvers
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

    public override void RunAlgorithm(CancellationToken cancellationToken)
    {
        InitializePopulation(StateManager, EvolutionObserver);
        var completed = false;
        try
        {
            for (var i = 0; i < MaxGenerations - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

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
                }

                if (newPopulation.Count > PopulationSize)
                {
                    newPopulation.Inhabitants.Sort((inhabitant1, inhabitant2) =>
                        inhabitant2.Fitness.CompareTo(inhabitant1.Fitness));
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

                if (i > TerminationEvaluationThreshold)
                    if (TerminationStrategy.Evaluate(EvolutionObserver))
                        break;

                Population = newPopulation;
                if (StateManager.PreviewLevel == 1) StateManager.GetDocument().ExpirePreview(true);
            }

            completed = !cancellationToken.IsCancellationRequested;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Solver error: {ex.Message}");
        }

        if (completed)
        {
            var best = Population.SelectTopIndividuals(1);
            best[0].Reinstate(StateManager);
        }
    }

    private List<Individual> PerformCrossover(IndividualPair individualPair)
    {
        return PerformOperation(individualPair, CrossoverStrategy.CrossoverRate, CrossoverStrategy.Crossover);
    }

    private void MutateChildren(List<Individual> children)
    {
        foreach (var child in children)
            MutationStrategy.Mutate(child);
    }

    private List<Individual> PerformOperation(IndividualPair individualPair, double rate,
        Func<Individual, Individual, List<Individual>> operation)
    {
        if (Random.NextDouble() < rate)
            return operation(individualPair.Individual1, individualPair.Individual2);
        return new List<Individual> { individualPair.Individual1, individualPair.Individual2 };
    }

}