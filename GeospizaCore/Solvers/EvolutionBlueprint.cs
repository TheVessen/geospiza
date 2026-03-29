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
    private const double RecoveryDecay = 0.95;
    private const double LowDiversityFraction = 0.4;
    protected readonly Random Random = new();
    private double _baseCrossoverRate;

    // Adaptive rate control — base values captured once at algorithm start.
    private double _baseMutationRate;

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
    }

    protected Population Population { get; set; } = new();
    protected int PopulationSize { get; set; }

    /// <summary>
    ///     Maximum of generations that should be run. The algorithm will stop after this number of generations or a
    ///     termination condition is met.
    /// </summary>
    protected int MaxGenerations { get; set; }

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
    ///     Initializes the first population for the evolutionary algorithm.
    /// </summary>
    public void InitializePopulation(StateManager stateManager, EvolutionObserver evolutionObserver)
    {
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

            if (stateManager.PreviewLevel == 0)
                stateManager.GetDocument().NewSolution(false);
            else
                stateManager.GetDocument().NewSolution(false, GH_SolutionMode.Silent);

            var currentFitness = fitnessInstance.GetFitness();

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

        evolutionObserver.Snapshot(newPopulation);
        Population = newPopulation;
        if (stateManager.PreviewLevel == 1)
        {
            stateManager.GetDocument().ExpirePreview(true);
            RhinoApp.Wait();
        }
    }

    /// <summary>
    ///     Records the initial mutation and crossover rates so <see cref="AdaptStrategies" /> can
    ///     decay back to them after a stagnation boost. Call once before the main loop.
    /// </summary>
    protected void CaptureBaseRates()
    {
        _baseMutationRate = MutationStrategy.MutationRate;
        _baseCrossoverRate = CrossoverStrategy.CrossoverRate;
    }

    /// <summary>
    ///     Adjusts mutation and crossover rates based on stagnation and diversity signals from
    ///     <paramref name="observer" />. Boosts rates when the best fitness is flat or unique
    ///     individuals fall below <see cref="LowDiversityFraction" /> of population size;
    ///     decays them back toward the base rates otherwise.
    /// </summary>
    protected void AdaptStrategies(EvolutionObserver observer)
    {
        if (observer.CurrentGenerationIndex < AdaptationWindow + 1) return;

        var recentBest = observer.BestFitness;
        var count = recentBest.Count;
        var windowMax = double.MinValue;
        var windowMin = double.MaxValue;
        for (var i = count - AdaptationWindow; i < count; i++)
        {
            if (recentBest[i] > windowMax) windowMax = recentBest[i];
            if (recentBest[i] < windowMin) windowMin = recentBest[i];
        }

        var isStagnating = windowMax - windowMin < 1e-9;

        var uniq = observer.NumberOfUniqueIndividuals;
        var lastUniq = uniq.Count > 0 ? uniq[uniq.Count - 1] : PopulationSize;
        var isDiversityLow = lastUniq < PopulationSize * LowDiversityFraction;

        if (isStagnating || isDiversityLow)
            MutationStrategy.MutationRate = Math.Min(
                MutationStrategy.MutationRate * StagnationBoost,
                _baseMutationRate * MaxMutationMultiplier);
        else
            MutationStrategy.MutationRate = Math.Max(
                MutationStrategy.MutationRate * RecoveryDecay,
                _baseMutationRate);

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
}