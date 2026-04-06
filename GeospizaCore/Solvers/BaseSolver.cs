using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Rhino;

namespace GeospizaCore.Solvers;

/// <summary>
///     Single-objective generational evolutionary algorithm with elitism and pluggable strategies
///     (selection, pairing, crossover, mutation, termination).
///     Reference: D. E. Goldberg, \"Genetic Algorithms in Search, Optimization, and
///     Machine Learning,\" Addison-Wesley, 1989.\n///
///     Uses a generational model where the entire population is replaced each iteration,
///     with elite individuals preserved to prevent fitness loss.
/// </summary>
public class BaseSolver : EvolutionBlueprint
{
    // Termination is skipped for the first N generations to avoid premature convergence detection.
    private const int TerminationEvaluationThreshold = 5;

    public BaseSolver(SolverSettings settings, StateManager stateManager,
        EvolutionObserver evolutionObserver) :
        base(settings)
    {
        StateManager = stateManager;
        EvolutionObserver = evolutionObserver;
        evolutionObserver.SetAlgorithmType(EvolutionObserver.AlgorithmType.SingleObjective);
    }

    private StateManager StateManager { get; }
    private EvolutionObserver EvolutionObserver { get; }

    public override void RunAlgorithm(CancellationToken cancellationToken)
    {
        InitializePopulation(StateManager, EvolutionObserver);
        CaptureBaseRates();
        var completed = false;
        try
        {
            for (var i = 0; i < MaxGenerations - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Snapshot the current population for selection; elite copies are taken from it directly.
                var selectionPool = new Population(Population);
                var newPopulation = new Population();

                var elite = Elitism.SelectTopIndividuals(EliteSize, Population.Inhabitants);
                newPopulation.AddIndividuals(elite);

                // Select only the individuals needed to fill the spots left after elitism.
                var matingPool = SelectionStrategy.Select(selectionPool, PopulationSize - newPopulation.Count);

                foreach (var pair in PairingStrategy.PairIndividuals(matingPool))
                {
                    if (newPopulation.Count >= PopulationSize) break;
                    var children = ApplyCrossover(pair);
                    MutateChildren(children);
                    newPopulation.AddIndividuals(children);
                }

                // The last pair may push count one over if both children were added; trim if so.
                if (newPopulation.Count > PopulationSize)
                    newPopulation.Inhabitants.RemoveRange(PopulationSize, newPopulation.Count - PopulationSize);

                foreach (var inhabitant in newPopulation.Inhabitants)
                    inhabitant.SetGeneration(i + 1);

                newPopulation.TestPopulation(StateManager, EvolutionObserver, elite.Count);

                StateManager.GetDocument().ExpirePreview(false);
                EvolutionObserver.Snapshot(newPopulation);
                AdaptStrategies(EvolutionObserver);

                //TODO: For multi processing here would be the point to send the observer to the main thread

                // Update before the termination check so the last evaluated generation is always current.
                Population = newPopulation;

                if (i > TerminationEvaluationThreshold && TerminationStrategy.Evaluate(EvolutionObserver))
                    break;

                if (StateManager.PreviewLevel == 1)
                {
                    StateManager.GetDocument().ExpirePreview(true);
                    RhinoApp.Wait();
                }
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

    private List<Individual> ApplyCrossover(IndividualPair pair)
    {
        if (Random.NextDouble() < CrossoverStrategy.CrossoverRate)
            return CrossoverStrategy.Crossover(pair.Individual1, pair.Individual2);
        return new List<Individual> { pair.Individual1, pair.Individual2 };
    }

    private void MutateChildren(List<Individual> children)
    {
        foreach (var child in children)
            MutationStrategy.Mutate(child);
    }
}