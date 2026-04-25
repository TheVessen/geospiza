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

        var completedNormally = false;
        try
        {
            for (var i = 0; i < EvolutionIterationCount; i++)
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

                // Survivors retain the ParetoRank assigned during sort of the combined pool, so
                // group by rank instead of re-running the O(M·N²) sort. Crowding distance is
                // (re)assigned per front for the tournament tiebreaker.
                var survivorFronts = nextPopulation.Inhabitants
                    .GroupBy(ind => ind.ParetoRank)
                    .OrderBy(g => g.Key)
                    .Select(g => g.ToList())
                    .ToList();
                foreach (var front in survivorFronts)
                    ParetoUtils.AssignCrowdingDistance(front, _objectiveCount);

                // Refresh ReferencePointIndex/Distance on the rank-0 front against the survivors'
                // own normalization so Reinstate (and any consumer) reads up-to-date values rather
                // than stale state from the combined pool's basis.
                if (survivorFronts.Count > 0)
                {
                    var rank0 = survivorFronts[0];
                    var rank0Norm = ParetoUtils.NormalizeObjectives(rank0, _objectiveCount);
                    ParetoUtils.AssociateToReferencePoints(rank0, rank0Norm, _referencePoints);
                }

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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NSGA-III Solver error: {ex.Message}");
        }

        // Reinstate the best individual only if the run completed normally (full loop or early
        // termination via the termination strategy). Skip on cancellation or after an exception,
        // since Population may then hold partial / stale state.
        if (completedNormally && Population.Inhabitants.Count > 0)
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

        // 5. Index critical-front candidates by reference point so the niche loop never has to
        //    scan the full candidate list. Each candidate index is offset by the count of
        //    already-selected (complete-front) members in nextPopulation.
        var candidateOffset = nextPopulation.Count;
        var candidatesByRef = new Dictionary<int, List<int>>();
        for (var i = 0; i < criticalFront.Count; i++)
        {
            var ci = candidateOffset + i;
            var r = refIndices[ci];
            if (!candidatesByRef.TryGetValue(r, out var list))
            {
                list = new List<int>();
                candidatesByRef[r] = list;
            }
            list.Add(ci);
        }

        // Track only reference points that still have at least one candidate. Bucketing them
        // by current niche count gives O(1) "find a reference point with the minimum niche".
        var refsByNiche = new Dictionary<int, HashSet<int>>();
        var minNiche = int.MaxValue;
        foreach (var r in candidatesByRef.Keys)
        {
            var nc = nicheCounts[r];
            if (!refsByNiche.TryGetValue(nc, out var bucket))
            {
                bucket = new HashSet<int>();
                refsByNiche[nc] = bucket;
            }
            bucket.Add(r);
            if (nc < minNiche) minNiche = nc;
        }

        // 6. Niche-preserving selection: iteratively pick from lowest-niche reference points.
        var totalCandidates = criticalFront.Count;
        for (var added = 0; added < needed && totalCandidates > 0; added++)
        {
            // Advance minNiche if its bucket has been emptied by previous iterations.
            while (!refsByNiche.TryGetValue(minNiche, out var bucketAtMin) || bucketAtMin.Count == 0)
                minNiche++;

            var eligible = refsByNiche[minNiche];
            // Pick one reference point at random from the eligible set.
            var targetRef = eligible.ElementAt(Random.Next(eligible.Count));
            var targetCandidates = candidatesByRef[targetRef];

            int chosen;
            int chosenSlot;
            if (minNiche == 0)
            {
                // Pick candidate with smallest perpendicular distance to the reference point.
                chosenSlot = 0;
                var minDist = distances[targetCandidates[0]];
                for (var k = 1; k < targetCandidates.Count; k++)
                {
                    var d = distances[targetCandidates[k]];
                    if (d < minDist)
                    {
                        minDist = d;
                        chosenSlot = k;
                    }
                }
                chosen = targetCandidates[chosenSlot];
            }
            else
            {
                chosenSlot = Random.Next(targetCandidates.Count);
                chosen = targetCandidates[chosenSlot];
            }

            // Remove chosen from its candidate bucket in O(1) via swap-with-last.
            var lastIdx = targetCandidates.Count - 1;
            targetCandidates[chosenSlot] = targetCandidates[lastIdx];
            targetCandidates.RemoveAt(lastIdx);
            totalCandidates--;

            nextPopulation.AddIndividual(allMembers[chosen]);

            // Update niche bookkeeping: targetRef just gained one occupant. Move it to the
            // (minNiche + 1) bucket; if it has no more candidates, drop it from the index.
            eligible.Remove(targetRef);
            nicheCounts[targetRef]++;

            if (targetCandidates.Count > 0)
            {
                var newNiche = nicheCounts[targetRef];
                if (!refsByNiche.TryGetValue(newNiche, out var newBucket))
                {
                    newBucket = new HashSet<int>();
                    refsByNiche[newNiche] = newBucket;
                }
                newBucket.Add(targetRef);
            }
            else
            {
                candidatesByRef.Remove(targetRef);
            }
        }

        return nextPopulation;
    }

    /// <summary>
    ///     Builds an offspring population of exactly <see cref="EvolutionBlueprint.PopulationSize" />
    ///     individuals using NSGA-III binary tournament selection (rank first, then crowding distance
    ///     as tiebreaker for compatibility). Children beyond the target size are simply not added,
    ///     so the result never has to be truncated.
    /// </summary>
    private Population CreateOffspring(CancellationToken cancellationToken)
    {
        var offspring = new Population();
        var selector = new NsgaIIITournamentSelection(Random);

        // Safety limit guards against pathological strategy combinations that fail to produce
        // any children (e.g. an empty mating pool); under normal use a single iteration suffices.
        var safetyLimit = PopulationSize * 10;
        while (offspring.Count < PopulationSize && !cancellationToken.IsCancellationRequested && safetyLimit-- > 0)
        {
            var stillNeeded = PopulationSize - offspring.Count;
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