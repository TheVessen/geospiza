using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;

namespace GeospizaCore.Solvers;

/// <summary>
///     NSGA-II multi-objective evolutionary solver (Deb et al. 2002).
///     Runs alongside <see cref="BaseSolver" /> without modifying it.
///     Requires a <c>GH_MultiObjectiveFitness</c> component on the Grasshopper canvas.
/// </summary>
public class NsgaIISolver : EvolutionBlueprint
{
    private const int TerminationEvaluationThreshold = 5;

    private int _objectiveCount;

    public NsgaIISolver(SolverSettings settings, StateManager stateManager,
        EvolutionObserver evolutionObserver) : base(settings)
    {
        StateManager = stateManager;
        EvolutionObserver = evolutionObserver;
    }

    private StateManager StateManager { get; }
    private EvolutionObserver EvolutionObserver { get; }

    public override void RunAlgorithm(CancellationToken cancellationToken)
    {
        _objectiveCount = InitializePopulationMultiObjective(StateManager, EvolutionObserver);
        var completed = false;

        try
        {
            for (var i = 0; i < MaxGenerations - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Create and test offspring
                var offspring = CreateOffspring();
                offspring.TestPopulationMultiObjective(StateManager, EvolutionObserver);

                // Combine parent + offspring
                var combined = new List<Individual>(Population.Inhabitants);
                combined.AddRange(offspring.Inhabitants);

                // Sort combined pool by Pareto rank, assign crowding distances
                var fronts = ParetoUtils.FastNonDominatedSort(combined);
                foreach (var front in fronts)
                    ParetoUtils.AssignCrowdingDistance(front, _objectiveCount);

                // Select next generation: fill fronts in rank order, truncate last if needed
                var nextPopulation = SelectNextGeneration(fronts);

                foreach (var inhabitant in nextPopulation.Inhabitants)
                    inhabitant.SetGeneration(i + 1);

                StateManager.GetDocument().ExpirePreview(false);
                EvolutionObserver.Snapshot(nextPopulation);

                if (i > TerminationEvaluationThreshold)
                    if (TerminationStrategy.Evaluate(EvolutionObserver))
                        break;

                Population = nextPopulation;
                if (StateManager.PreviewLevel == 1) StateManager.GetDocument().ExpirePreview(true);
            }

            completed = !cancellationToken.IsCancellationRequested;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NSGA-II Solver error: {ex.Message}");
        }

        if (completed)
        {
            // Reinstate best individual from rank-0 front; prefer highest crowding distance for diversity
            var best = Population.Inhabitants
                .Where(ind => ind.ParetoRank == 0)
                .OrderByDescending(ind => ind.CrowdingDistance)
                .FirstOrDefault() ?? Population.Inhabitants[0];
            best.Reinstate(StateManager);
        }
    }

    /// <summary>
    ///     Initializes the first population by randomizing genes and reading multi-objective fitness.
    /// </summary>
    /// <returns>The number of objectives detected from the first evaluation.</returns>
    private int InitializePopulationMultiObjective(StateManager stateManager, EvolutionObserver evolutionObserver)
    {
        var fitnessInstance = Fitness.Instance;
        var newPopulation = new Population();
        var objectiveCount = 0;

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

            var objectives = fitnessInstance.GetObjectives();
            objectiveCount = objectives.Length;
            individual.SetObjectives(objectives);
            if (objectives.Length > 0) individual.SetFitness(objectives[0]);
            individual.SetGeneration(0);
            newPopulation.AddIndividual(individual);
        }

        // Initial Pareto sort
        if (objectiveCount > 0)
        {
            var fronts = ParetoUtils.FastNonDominatedSort(newPopulation.Inhabitants);
            foreach (var front in fronts)
                ParetoUtils.AssignCrowdingDistance(front, objectiveCount);
        }

        evolutionObserver.Snapshot(newPopulation);
        Population = newPopulation;
        if (stateManager.PreviewLevel == 1) stateManager.GetDocument().ExpirePreview(true);

        return objectiveCount;
    }

    /// <summary>
    ///     Builds an offspring population of size <see cref="EvolutionBlueprint.PopulationSize" />
    ///     using binary tournament selection (rank / crowding distance), pairing, crossover, and mutation.
    /// </summary>
    private Population CreateOffspring()
    {
        var offspring = new Population();
        var selector = new NsgaIITournamentSelection(Random);

        while (offspring.Count < PopulationSize)
        {
            var matingPool = selector.Select(Population, PopulationSize);
            var pairs = PairingStrategy.PairIndividuals(matingPool);

            foreach (var pair in pairs)
            {
                var children = PerformCrossover(pair);
                MutateChildren(children);
                offspring.AddIndividuals(children);
                if (offspring.Count >= PopulationSize) break;
            }
        }

        if (offspring.Count > PopulationSize)
            offspring.Inhabitants.RemoveRange(PopulationSize, offspring.Count - PopulationSize);

        return offspring;
    }

    /// <summary>
    ///     Fills the next generation from sorted fronts, truncating the last front by
    ///     crowding distance (descending) when it does not fit entirely.
    /// </summary>
    private Population SelectNextGeneration(List<List<Individual>> fronts)
    {
        var nextPopulation = new Population();

        foreach (var front in fronts)
        {
            if (nextPopulation.Count + front.Count <= PopulationSize)
            {
                nextPopulation.AddIndividuals(front);
            }
            else
            {
                var needed = PopulationSize - nextPopulation.Count;
                var sorted = front
                    .OrderByDescending(ind => ind.CrowdingDistance)
                    .Take(needed)
                    .ToList();
                nextPopulation.AddIndividuals(sorted);
                break;
            }

            if (nextPopulation.Count >= PopulationSize) break;
        }

        return nextPopulation;
    }

    private List<Individual> PerformCrossover(IndividualPair pair)
    {
        if (Random.NextDouble() < CrossoverStrategy.CrossoverRate)
            return CrossoverStrategy.Crossover(pair.Individual1, pair.Individual2);
        return new List<Individual> { pair.Individual1, pair.Individual2 };
    }

    private void MutateChildren(List<Individual> children)
    {
        foreach (var child in children)
            if (Random.NextDouble() < MutationStrategy.MutationRate)
                MutationStrategy.Mutate(child);
    }

    /// <summary>
    ///     Binary tournament selection that compares by Pareto rank (ascending) then
    ///     crowding distance (descending), matching NSGA-II crowded comparison operator.
    /// </summary>
    private sealed class NsgaIITournamentSelection : ISelectionStrategy
    {
        private readonly Random _random;

        public NsgaIITournamentSelection(Random random)
        {
            _random = random;
        }

        public List<Individual> Select(Population population, int numberOfSelections)
        {
            var selected = new List<Individual>(numberOfSelections);
            var inhabitants = population.Inhabitants;

            for (var i = 0; i < numberOfSelections; i++)
            {
                var a = inhabitants[_random.Next(inhabitants.Count)];
                var b = inhabitants[_random.Next(inhabitants.Count)];
                selected.Add(IsBetter(a, b) ? a : b);
            }

            return selected;
        }

        private static bool IsBetter(Individual a, Individual b)
        {
            if (a.ParetoRank < b.ParetoRank) return true;
            if (a.ParetoRank > b.ParetoRank) return false;
            return a.CrowdingDistance >= b.CrowdingDistance;
        }
    }
}
