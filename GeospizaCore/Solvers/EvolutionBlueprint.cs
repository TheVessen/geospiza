using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;
using Rhino;

namespace GeospizaCore.Solvers;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
    private const int AdaptationWindow = 5;
    private const double StagnationBoost = 1.5;
    private const double MaxMutationMultiplier = 3.0;
    private const double RecoveryDecay = 0.9;
    private const double LowDiversityFractionSingleObjective = 0.3;
    private const double LowDiversityFractionMultiObjective = 0.6;

    // Mutation schedule: the scheduled base rate decays linearly from the user-configured rate
    // down to this fraction of it by the final generation (exploration early, refinement late).
    private const double MutationScheduleFloorFraction = 0.3;

    // Random immigrants: when the previous generation's genotypic diversity (mean pairwise
    // normalized gene distance) falls below the threshold, this fraction of the next
    // generation is replaced with fresh random individuals.
    private const double LowGenotypicDiversityThreshold = 0.10;
    private const double ImmigrantFraction = 0.10;
    protected readonly Random Random = new();
    private double _baseCrossoverRate;

    // Adaptive rate control — base values captured once at algorithm start.
    private double _baseMutationRate;

    // Captured at construction; controls whether a per-run FitnessCache is attached to the
    // StateManager during Initialize* so duplicate genotypes skip the Grasshopper solve.
    private readonly bool _useFitnessCache;

    /// <summary>
    ///     Initializes the evolutionary algorithm with the given settings.
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
        _useFitnessCache = settings.UseFitnessCache;
    }

    /// <summary>
    ///     Sets up or tears down the per-run fitness cache on <paramref name="stateManager" />
    ///     according to the solver settings. Called once at the start of each run before the
    ///     initial population is evaluated, so that even gen-0 results populate the cache for
    ///     subsequent generations to hit.
    /// </summary>
    private void ConfigureFitnessCache(StateManager stateManager)
    {
        if (_useFitnessCache)
        {
            if (stateManager.FitnessCache == null)
                stateManager.FitnessCache = new FitnessCache();
            else
                stateManager.FitnessCache.Clear();
        }
        else
        {
            stateManager.FitnessCache = null;
        }
    }

    protected Population Population { get; set; } = new();
    protected int PopulationSize { get; set; }

    /// <summary>
    ///     Maximum of generations that should be run. The algorithm will stop after this number of generations or a
    ///     termination condition is met.
    /// </summary>
    protected int MaxGenerations { get; set; }

    /// <summary>
    ///     Number of evolution iterations the main loop should run. Generation 0 is the initial
    ///     population produced by <see cref="InitializePopulation"/> /
    ///     <see cref="InitializePopulationMultiObjective"/>; subsequent iterations produce
    ///     generations 1 through MaxGenerations - 1, for a total of <see cref="MaxGenerations"/>
    ///     generations when termination does not fire early.
    /// </summary>
    protected int EvolutionIterationCount => Math.Max(0, MaxGenerations - 1);

    /// <summary>
    ///     The number of the best individuals that should be preserved for the next generation.
    /// </summary>
    protected int EliteSize { get; set; }

    /// <summary>
    ///     Selection strategy that should be used for the evolutionary algorithm.
    /// </summary>
    protected ISelectionStrategy SelectionStrategy { get; set; }

    /// <summary>
    ///     Crossover strategy that should be used for the evolutionary algorithm.
    /// </summary>
    protected ICrossoverStrategy CrossoverStrategy { get; set; }

    /// <summary>
    ///     Mutation strategy that should be used for the evolutionary algorithm.
    /// </summary>
    protected IMutationStrategy MutationStrategy { get; set; }

    /// <summary>
    ///     Pairing strategy that should be used for the evolutionary algorithm.
    /// </summary>
    protected IPairingStrategy PairingStrategy { get; set; }

    /// <summary>
    ///     Termination strategy that should be used for the evolutionary algorithm.
    /// </summary>
    protected ITerminationStrategy TerminationStrategy { get; set; }

    /// <summary>
    ///     Main method to run the evolutionary algorithm.
    /// </summary>
    public abstract void RunAlgorithm(CancellationToken cancellationToken);

    /// <summary>
    ///     Initializes the first population for multi-objective solvers (NSGA-II / NSGA-III).
    ///     Randomizes genes, evaluates objectives via <see cref="Fitness.Instance" />, runs an
    ///     initial Pareto sort, assigns crowding distances, and snapshots generation 0.
    /// </summary>
    /// <returns>The number of objectives detected from the first evaluation.</returns>
    protected int InitializePopulationMultiObjective(StateManager stateManager,
        EvolutionObserver evolutionObserver)
    {
        ConfigureFitnessCache(stateManager);
        var cache = stateManager.FitnessCache;

        var fitnessInstance = Fitness.Instance;
        var newPopulation = new Population();
        var objectiveCount = 0;

        for (var i = 0; i < PopulationSize; i++)
        {
            var individual = new Individual();

            foreach (var geneTemplate in stateManager.Genotype)
            {
                var ctg = geneTemplate.Value;
                ctg.SetTickValue(Random.Next(ctg.TickCount + 1), stateManager);

                var stableGene = new Gene(ctg.TickValue, ctg.GeneGuid,
                    ctg.TickCount, ctg.Name, ctg.GhInstanceGuid,
                    ctg.GenePoolIndex);

                individual.AddGene(stableGene);
            }

            // Cache hit on the initial sweep is rare but possible (random duplicates); skip the
            // Grasshopper solve when we already have an answer for this exact tick sequence.
            if (cache != null && cache.TryGet(individual.GenePool, out var cached))
            {
                if (cached.Objectives != null)
                {
                    individual.SetObjectives(cached.Objectives);
                    objectiveCount = cached.Objectives.Length;
                }
                individual.SetFitness(cached.Fitness);
                individual.SetGeneration(0);
                newPopulation.AddIndividual(individual);
                continue;
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

            // Populate the cache so duplicates that re-appear via crossover in subsequent
            // generations skip the GH solve.
            cache?.Store(individual.GenePool, individual.Fitness, individual.Objectives);
        }

        if (objectiveCount > 0)
        {
            var fronts = ParetoUtils.FastNonDominatedSort(newPopulation.Inhabitants);
            foreach (var front in fronts)
                ParetoUtils.AssignCrowdingDistance(front, objectiveCount);
        }

        evolutionObserver.Snapshot(newPopulation, stateManager);
        Population = newPopulation;

        if (stateManager.PreviewLevel == 1)
        {
            stateManager.GetDocument().ExpirePreview(true);
            RhinoApp.Wait();
        }

        return objectiveCount;
    }

    /// <summary>
    ///     Initializes the first population for the evolutionary algorithm.
    /// </summary>
    public void InitializePopulation(StateManager stateManager, EvolutionObserver evolutionObserver)
    {
        ConfigureFitnessCache(stateManager);
        var cache = stateManager.FitnessCache;

        var firstGenBestFitness = 0.0;
        var fitnessInstance = Fitness.Instance;

        var newPopulation = new Population();
        for (var i = 0; i < PopulationSize; i++)
        {
            var individual = new Individual();

            foreach (var geneTemplate in stateManager.Genotype)
            {
                var ctg = geneTemplate.Value;
                ctg.SetTickValue(Random.Next(ctg.TickCount + 1), stateManager);

                var stableGene = new Gene(ctg.TickValue, ctg.GeneGuid,
                    ctg.TickCount, ctg.Name, ctg.GhInstanceGuid,
                    ctg.GenePoolIndex);

                individual.AddGene(stableGene);
            }

            double currentFitness;
            // Cache hit on the initial sweep is rare but possible (random duplicates); skip the
            // Grasshopper solve when we already have an answer for this exact tick sequence.
            if (cache != null && cache.TryGet(individual.GenePool, out var cached))
            {
                currentFitness = cached.Fitness;
            }
            else
            {
                if (stateManager.PreviewLevel == 0)
                    stateManager.GetDocument().NewSolution(false);
                else
                    stateManager.GetDocument().NewSolution(false, GH_SolutionMode.Silent);

                currentFitness = fitnessInstance.GetFitness();
                // Populate the cache so duplicates that re-appear via crossover in subsequent
                // generations skip the GH solve.
                cache?.Store(individual.GenePool, currentFitness, null);
            }

            if (i == 0)
            {
                firstGenBestFitness = currentFitness;
            }
            else if (currentFitness > firstGenBestFitness)
            {
                firstGenBestFitness = currentFitness;
                if (stateManager.PreviewLevel == 2)
                {
                    stateManager.GetDocument().ExpirePreview(true);
                    RhinoApp.Wait();
                }
            }

            individual.SetFitness(currentFitness);
            individual.SetGeneration(0);
            newPopulation.AddIndividual(individual);
        }

        evolutionObserver.Snapshot(newPopulation, stateManager);
        Population = newPopulation;
        if (stateManager.PreviewLevel == 1)
        {
            stateManager.GetDocument().ExpirePreview(true);
            RhinoApp.Wait();
        }
    }

    /// <summary>
    ///     Resets strategy rates to the user-configured values and records them as the base
    ///     for <see cref="AdaptStrategies" /> decay. Call once before the main loop.
    ///     This undoes any rate drift left on the shared strategy objects from a previous run.
    /// </summary>
    protected void CaptureBaseRates()
    {
        // Read the initial rate stored on the strategy at construction time — this is immune
        // to drift from AdaptStrategies regardless of how many times the same strategy object
        // has been reused across consecutive runs.
        var initialMutation = (MutationStrategy as MutationStrategy)?.InitialMutationRate
                              ?? MutationStrategy.MutationRate;
        var initialCrossover = (CrossoverStrategy as CrossoverStrategy)?.InitialCrossoverRate
                               ?? CrossoverStrategy.CrossoverRate;

        MutationStrategy.MutationRate = initialMutation;
        CrossoverStrategy.CrossoverRate = initialCrossover;
        _baseMutationRate = initialMutation;
        _baseCrossoverRate = initialCrossover;
    }

    /// <summary>
    ///     Adjusts mutation and crossover rates based on stagnation and diversity signals from
    ///     <paramref name="observer" />. Boosts rates when the population is stagnating or unique
    ///     individuals fall below <see cref="LowDiversityFraction" /> of population size;
    ///     decays them back toward the base rates otherwise.
    ///     For multi-objective runs, stagnation is detected via hypervolume (Pareto front not growing);
    ///     for single-objective runs, stagnation is detected via best fitness flatness.
    /// </summary>
    protected void AdaptStrategies(EvolutionObserver observer)
    {
        bool isStagnating;
        var isMultiObjective = observer.Algorithm != EvolutionObserver.AlgorithmType.SingleObjective;

        // Guard on the actual signal list length rather than the generation index — they can
        // diverge if a generation throws before its snapshot is recorded.
        if (isMultiObjective)
        {
            var hv = observer.Hypervolume;
            var count = hv.Count;
            if (count < AdaptationWindow) return;
            var windowMax = double.MinValue;
            var windowMin = double.MaxValue;
            for (var i = count - AdaptationWindow; i < count; i++)
            {
                if (hv[i] > windowMax) windowMax = hv[i];
                if (hv[i] < windowMin) windowMin = hv[i];
            }
            // Stagnating if hypervolume hasn't grown by at least a relative 0.1% over the window.
            isStagnating = windowMax <= 0 || (windowMax - windowMin) / windowMax < 1e-3;
        }
        else
        {
            var recentBest = observer.BestFitness;
            var count = recentBest.Count;
            if (count < AdaptationWindow) return;
            var windowMax = double.MinValue;
            var windowMin = double.MaxValue;
            for (var i = count - AdaptationWindow; i < count; i++)
            {
                if (recentBest[i] > windowMax) windowMax = recentBest[i];
                if (recentBest[i] < windowMin) windowMin = recentBest[i];
            }
            isStagnating = windowMax - windowMin < 1e-9;
        }

        var uniq = observer.NumberOfUniqueIndividuals;
        var lastUniq = uniq.Count > 0 ? uniq[uniq.Count - 1] : PopulationSize;
        var diversityThreshold = isMultiObjective
            ? LowDiversityFractionMultiObjective
            : LowDiversityFractionSingleObjective;
        // Genotypic diversity collapses before the unique count does (individuals stay distinct
        // while clustering ever tighter), so it is checked alongside the unique-count fraction.
        var genoDiv = observer.GenotypicDiversity;
        var lastGenoDiv = genoDiv.Count > 0 ? genoDiv[genoDiv.Count - 1] : double.MaxValue;
        var isDiversityLow = lastUniq < PopulationSize * diversityThreshold
                             || lastGenoDiv < LowGenotypicDiversityThreshold;

        // The boost/decay anchors follow a decaying schedule rather than the flat base rate:
        // large mutation steps early in the run (exploration), smaller ones near the end
        // (refinement). Stagnation/diversity boosts apply on top of the scheduled level.
        var scheduledMutationRate = _baseMutationRate * ScheduleFactor(observer);

        if (isStagnating || isDiversityLow)
            MutationStrategy.MutationRate = Math.Min(
                MutationStrategy.MutationRate * StagnationBoost,
                Math.Min(scheduledMutationRate * MaxMutationMultiplier, 1.0));
        else
            MutationStrategy.MutationRate = Math.Max(
                MutationStrategy.MutationRate * RecoveryDecay,
                scheduledMutationRate);

        // Also adapt crossover rate: boost on stagnation or low diversity, decay otherwise.
        if (isStagnating || isDiversityLow)
            CrossoverStrategy.CrossoverRate = Math.Min(
                CrossoverStrategy.CrossoverRate * StagnationBoost,
                1.0);
        else
            CrossoverStrategy.CrossoverRate = Math.Max(
                CrossoverStrategy.CrossoverRate * RecoveryDecay,
                _baseCrossoverRate);
    }

    /// <summary>
    ///     Linear decay factor for the mutation schedule: 1.0 at generation 0 down to
    ///     <see cref="MutationScheduleFloorFraction" /> at the final generation.
    /// </summary>
    private double ScheduleFactor(EvolutionObserver observer)
    {
        if (MaxGenerations <= 1) return 1.0;
        var progress = Math.Min(1.0, observer.CurrentGenerationIndex / (double)(MaxGenerations - 1));
        return 1.0 - (1.0 - MutationScheduleFloorFraction) * progress;
    }

    /// <summary>
    ///     Replaces the trailing <see cref="ImmigrantFraction" /> of <paramref name="population" />
    ///     with fresh random individuals when the previous generation's genotypic diversity fell
    ///     below <see cref="LowGenotypicDiversityThreshold" />. The first
    ///     <paramref name="protectedCount" /> individuals (elites) are never replaced.
    ///     Must be called before the population is evaluated, so the immigrants get tested
    ///     along with the rest of the generation.
    /// </summary>
    protected void InjectImmigrantsIfDiversityLow(Population population, StateManager stateManager,
        EvolutionObserver observer, int protectedCount = 0)
    {
        var diversity = observer.GenotypicDiversity;
        if (diversity.Count == 0 || diversity[diversity.Count - 1] >= LowGenotypicDiversityThreshold)
            return;

        var immigrantCount = Math.Max(1, (int)(PopulationSize * ImmigrantFraction));
        var inhabitants = population.Inhabitants;
        for (var idx = inhabitants.Count - 1;
             idx >= protectedCount && immigrantCount > 0;
             idx--, immigrantCount--)
            inhabitants[idx] = CreateRandomIndividual(stateManager);
    }

    /// <summary>
    ///     Builds an unevaluated individual with uniformly random tick values drawn from the
    ///     genotype templates. The Grasshopper document is not touched — evaluation happens
    ///     later through the normal population test.
    /// </summary>
    private Individual CreateRandomIndividual(StateManager stateManager)
    {
        var individual = new Individual();
        foreach (var geneTemplate in stateManager.Genotype)
        {
            var ctg = geneTemplate.Value;
            individual.AddGene(new Gene(Random.Next(ctg.TickCount + 1), ctg.GeneGuid,
                ctg.TickCount, ctg.Name, ctg.GhInstanceGuid, ctg.GenePoolIndex));
        }

        return individual;
    }
}