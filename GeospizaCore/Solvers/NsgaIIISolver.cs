using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;
using Rhino;

namespace GeospizaCore.Solvers;

/// <summary>
///     NSGA-III: many-objective evolutionary algorithm using reference-point-based
///     nondominated sorting approach (Deb &amp; Jain, 2014).
///     Reference: K. Deb and H. Jain, "An evolutionary many-objective optimization algorithm
///     using reference-point-based nondominated sorting approach, Part I: Solving problems with
///     box constraints," IEEE Transactions on Evolutionary Computation, vol. 18, no. 4,
///     pp. 577–601, Aug. 2014, doi: 10.1109/TEVC.2013.2281534.
///     Extends NSGA-II by replacing crowding-distance survivor selection with
///     structured reference-point-based niche preservation, which maintains better
///     diversity when there are four or more objectives.
///     Requires a <c>GH_MultiObjectiveFitness</c> component on the Grasshopper canvas.
/// </summary>
public class NsgaIIISolver : EvolutionBlueprint
{
    private const int TerminationEvaluationThreshold = 5;

    private readonly int _referencePointDivisions;
    private int _objectiveCount;
    private List<double[]> _referencePoints = new();

    public NsgaIIISolver(SolverSettings settings, StateManager stateManager,
        EvolutionObserver evolutionObserver, int referencePointDivisions = 12) : base(settings)
    {
        StateManager = stateManager;
        EvolutionObserver = evolutionObserver;
        _referencePointDivisions = referencePointDivisions;
        PairingStrategy = new ReferencePointPairingStrategy();
        evolutionObserver.SetAlgorithmType(EvolutionObserver.AlgorithmType.NsgaIII);
        evolutionObserver.SetSettings(settings);
    }

    private StateManager StateManager { get; }
    private EvolutionObserver EvolutionObserver { get; }

    public override void RunAlgorithm(CancellationToken cancellationToken)
    {
        _objectiveCount = InitializePopulationMultiObjective(StateManager, EvolutionObserver);
        CaptureBaseRates();

        _referencePoints = ParetoUtils.GenerateReferencePoints(_objectiveCount, _referencePointDivisions);

        try
        {
            for (var i = 0; i < MaxGenerations - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Create and evaluate offspring.
                var offspring = CreateOffspring(cancellationToken);
                if (cancellationToken.IsCancellationRequested) break;
                offspring.TestPopulationMultiObjective(StateManager, EvolutionObserver);

                // Combine parent + offspring into combined pool.
                var combined = new List<Individual>(Population.Inhabitants);
                combined.AddRange(offspring.Inhabitants);

                // Pareto sort — same as NSGA-II.
                var fronts = ParetoUtils.FastNonDominatedSort(combined);

                // Select next generation using reference-point niche preservation.
                var nextPopulation = SelectNextGeneration(fronts);

                foreach (var inhabitant in nextPopulation.Inhabitants)
                    inhabitant.SetGeneration(i + 1);

                // Assign crowding distance per front so the tournament tiebreaker works.
                var nextFronts = ParetoUtils.FastNonDominatedSort(nextPopulation.Inhabitants);
                foreach (var front in nextFronts)
                    ParetoUtils.AssignCrowdingDistance(front, _objectiveCount);

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

        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NSGA-III Solver error: {ex.Message}");
        }

        // Reinstate the best individual whenever the run was not explicitly cancelled by the user.
        // This covers both normal completion and early termination via a termination strategy.
        if (!cancellationToken.IsCancellationRequested)
        {
            // Reinstate the rank-0 individual with the smallest perpendicular distance to its
            // reference point (best-represented point on the Pareto front).
            var best = Population.Inhabitants
                .Where(ind => ind.ParetoRank == 0)
                .OrderBy(ind => ind.ReferencePointDistance)
                .FirstOrDefault() ?? Population.Inhabitants[0];
            best.Reinstate(StateManager);
        }
    }

    /// <summary>
    ///     Selects the next generation from the sorted fronts using NSGA-III's
    ///     reference-point-based niche preservation for the critical (last) front.
    /// </summary>
    private Population SelectNextGeneration(List<List<Individual>> fronts)
    {
        var nextPopulation = new Population();
        List<Individual>? criticalFront = null;

        // 1. Fill complete fronts in rank order.
        foreach (var front in fronts)
            if (nextPopulation.Count + front.Count <= PopulationSize)
            {
                nextPopulation.AddIndividuals(front);
                if (nextPopulation.Count == PopulationSize) return nextPopulation;
            }
            else
            {
                criticalFront = front;
                break;
            }

        if (criticalFront == null || nextPopulation.Count >= PopulationSize)
            return nextPopulation;

        var needed = PopulationSize - nextPopulation.Count;

        // 2. Normalize objectives across all members (next + critical).
        var allMembers = new List<Individual>(nextPopulation.Inhabitants);
        allMembers.AddRange(criticalFront);

        var normalized = ParetoUtils.NormalizeObjectives(allMembers, _objectiveCount);

        // 3. Associate everyone to reference points.
        var (refIndices, distances) = ParetoUtils.AssociateToReferencePoints(
            allMembers, normalized, _referencePoints);

        // 4. Compute niche counts from already-selected members (those in nextPopulation).
        var nicheCounts = new int[_referencePoints.Count];
        for (var i = 0; i < nextPopulation.Count; i++)
            nicheCounts[refIndices[i]]++;

        // 5. Build working list of candidates from the critical front (indices offset by nextPop count).
        var candidateOffset = nextPopulation.Count;
        var candidates = new List<int>(criticalFront.Count);
        for (var i = 0; i < criticalFront.Count; i++)
            candidates.Add(candidateOffset + i);

        // 6. Niche-preserving selection: iteratively pick from lowest-niche reference points.
        for (var added = 0; added < needed && candidates.Count > 0; added++)
        {
            // Find the minimum niche count among reference points that have at least one candidate.
            var minNiche = int.MaxValue;
            foreach (var ci in candidates)
            {
                var rc = nicheCounts[refIndices[ci]];
                if (rc < minNiche) minNiche = rc;
            }

            // Collect reference points with that niche count that have candidates.
            var eligibleRefs = new HashSet<int>();
            foreach (var ci in candidates)
                if (nicheCounts[refIndices[ci]] == minNiche)
                    eligibleRefs.Add(refIndices[ci]);

            // Pick one reference point at random from the eligible set.
            var targetRef = eligibleRefs.ElementAt(Random.Next(eligibleRefs.Count));

            // Among candidates associated with targetRef, pick the one with minimum distance
            // when niche count is 0; otherwise pick randomly.
            var targetCandidates = candidates.Where(ci => refIndices[ci] == targetRef).ToList();

            int chosen;
            if (minNiche == 0)
            {
                // Pick candidate with smallest perpendicular distance to the reference point.
                chosen = targetCandidates[0];
                var minDist = distances[chosen];
                for (var k = 1; k < targetCandidates.Count; k++)
                {
                    var d = distances[targetCandidates[k]];
                    if (d < minDist)
                    {
                        minDist = d;
                        chosen = targetCandidates[k];
                    }
                }
            }
            else
            {
                chosen = targetCandidates[Random.Next(targetCandidates.Count)];
            }

            nextPopulation.AddIndividual(allMembers[chosen]);
            nicheCounts[targetRef]++;
            candidates.Remove(chosen);
        }

        return nextPopulation;
    }

    /// <summary>
    ///     Builds an offspring population using the NSGA-III binary tournament selection
    ///     (rank first, then crowding distance as tiebreaker for compatibility).
    /// </summary>
    private Population CreateOffspring(CancellationToken cancellationToken)
    {
        var offspring = new Population();
        var selector = new NsgaIIITournamentSelection(Random);

        var safetyLimit = PopulationSize * 10;
        while (offspring.Count < PopulationSize && !cancellationToken.IsCancellationRequested && safetyLimit-- > 0)
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

    private List<Individual> PerformCrossover(IndividualPair pair)
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

    /// <summary>
    ///     Binary tournament selection using Pareto rank then crowding distance as tiebreaker,
    ///     matching the NSGA-II crowded comparison operator used for offspring generation in NSGA-III.
    /// </summary>
    private sealed class NsgaIIITournamentSelection : ISelectionStrategy
    {
        private readonly Random _random;

        public NsgaIIITournamentSelection(Random random)
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