using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;
using Rhino;

namespace GeospizaCore.Solvers;

/// <summary>
///     NSGA-II: nondominated sorting genetic algorithm II, a fast and elitist
///     multi-objective evolutionary algorithm (Deb et al., 2002).
///     Reference: K. Deb, A. Pratap, S. Agarwal, and T. Meyarivan, "A fast and elitist
///     multiobjective genetic algorithm: NSGA-II," IEEE Transactions on Evolutionary
///     Computation, vol. 6, no. 2, pp. 182–197, Apr. 2002,
///     doi: 10.1109/4235.996017.
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
        PairingStrategy = new RankAwarePairingStrategy();
        evolutionObserver.SetAlgorithmType(EvolutionObserver.AlgorithmType.NsgaII);
        evolutionObserver.SetSettings(settings);
    }

    private StateManager StateManager { get; }
    private EvolutionObserver EvolutionObserver { get; }

    public override void RunAlgorithm(CancellationToken cancellationToken)
    {
        _objectiveCount = InitializePopulationMultiObjective(StateManager, EvolutionObserver);
        if (_objectiveCount == 0)
            throw new InvalidOperationException(
                "The initial population produced no objective values. " +
                "Connect a Multi-Objective Fitness component with at least one objective.");
        CaptureBaseRates();
        var completedNormally = false;
        try
        {
            for (var i = 0; i < EvolutionIterationCount; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Create and test offspring
                var offspring = CreateOffspring(cancellationToken);
                if (cancellationToken.IsCancellationRequested) break;

                // On diversity collapse, swap trailing offspring for random immigrants; the
                // combined-pool sort below culls them again unless they open new regions.
                InjectImmigrantsIfDiversityLow(offspring, StateManager, EvolutionObserver);

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

                // Assign Population before snapshot so Reinstate uses the correct generation
                // even when termination fires immediately after.
                Population = nextPopulation;

                StateManager.GetDocument().ExpirePreview(false);
                EvolutionObserver.Snapshot(nextPopulation, StateManager);
                AdaptStrategies(EvolutionObserver);

                if (i > TerminationEvaluationThreshold)
                    if (TerminationStrategy.Evaluate(EvolutionObserver))
                        break;
                if (StateManager.PreviewLevel == 1)
                {
                    StateManager.GetDocument().ExpirePreview(true);
                    RhinoApp.Wait();
                }
            }

            completedNormally = !cancellationToken.IsCancellationRequested;
        }
        catch (Exception)
        {
            // Propagate so the calling component can report the failure to the user;
            // completedNormally stays false, so partial state is never reinstated.
            throw;
        }

        // Reinstate the best individual only if the run completed normally (full loop or early
        // termination via the termination strategy). Skip on cancellation or after an exception,
        // since Population may then hold partial / stale state.
        if (completedNormally && Population.Inhabitants.Count > 0)
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
    ///     Builds an offspring population of exactly <see cref="EvolutionBlueprint.PopulationSize" />
    ///     individuals using binary tournament selection (rank / crowding distance), pairing,
    ///     crossover, and mutation. Children beyond the target size are simply not added,
    ///     so the result never has to be truncated.
    /// </summary>
    private Population CreateOffspring(CancellationToken cancellationToken)
    {
        var offspring = new Population();
        var selector = new NsgaIITournamentSelection(Random);

        // Safety limit guards against pathological strategy combinations that fail to produce
        // any children (e.g. an empty mating pool); under normal use a single iteration suffices.
        var safetyLimit = PopulationSize * 10;
        while (offspring.Count < PopulationSize && !cancellationToken.IsCancellationRequested && safetyLimit-- > 0)
        {
            var stillNeeded = PopulationSize - offspring.Count;
            // Each pair produces up to 2 children; keep the pool small enough to avoid wasted work
            // late in the loop, but never below 2 so PairIndividuals can form at least one pair.
            var poolSize = Math.Max(2, stillNeeded);
            var matingPool = selector.Select(Population, poolSize);
            // Materialize once: PairIndividuals implementations are typically yield-based,
            // and we both need a count check and a single-pass enumeration below.
            var pairs = PairingStrategy.PairIndividuals(matingPool).ToList();

            if (pairs.Count == 0) break;

            foreach (var pair in pairs)
            {
                if (offspring.Count >= PopulationSize) break;
                var children = PerformCrossover(pair);
                MutateChildren(children);
                foreach (var child in children)
                {
                    if (offspring.Count >= PopulationSize) break;
                    offspring.AddIndividual(child);
                }
            }
        }

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
        return new List<Individual> { new Individual(pair.Individual1.GenePool), new Individual(pair.Individual2.GenePool) };
    }

    private void MutateChildren(List<Individual> children)
    {
        foreach (var child in children)
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
            var n = inhabitants.Count;

            for (var i = 0; i < numberOfSelections; i++)
            {
                var ai = _random.Next(n);
                var bi = _random.Next(n);
                // Avoid degenerate self-comparison (which would always pick the same individual)
                // when the population has at least two distinct slots.
                if (n > 1)
                    while (bi == ai) bi = _random.Next(n);
                var a = inhabitants[ai];
                var b = inhabitants[bi];
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