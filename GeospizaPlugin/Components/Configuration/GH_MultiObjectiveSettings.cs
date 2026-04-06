using System;
using System.Drawing;
using GeospizaCore.Solvers;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Configuration;

/// <summary>
///     Settings component for multi-objective solvers (NSGA-II, NSGA-III).
///     Defaults to <see cref="RankAwarePairingStrategy" /> and rejects strategies
///     that are incompatible with multi-objective optimization.
/// </summary>
public class GH_MultiObjectiveSettings : GH_Component
{
    public GH_MultiObjectiveSettings()
        : base("Multi-Objective Settings", "MOSettings",
            "Configure parameters and strategies for NSGA-II and NSGA-III multi-objective solvers.",
            "Geospiza", "Configuration")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.Settings;

    public override Guid ComponentGuid => new("63C53775-8907-48DB-A83F-54EFAD3F6301");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Population Size", "PS", "The size of the population", GH_ParamAccess.item, 50);
        pManager.AddNumberParameter("Max Generations", "MG", "The maximum number of generations",
            GH_ParamAccess.item, 50);
        pManager.AddNumberParameter("Elite Size", "ES",
            "The number of elite individuals. If 0 no elite will be picked", GH_ParamAccess.item, 1);
        pManager.AddGenericParameter("Selection Strategy", "SS",
            "The selection strategy. Default: TournamentSelection with size 3.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Pairing Strategy", "PA",
            "The pairing strategy. Default: RankAwarePairing (recommended for NSGA-II/III).",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("Crossover Strategy", "CS",
            "The crossover strategy. Default: TwoPointCrossover with rate 0.7.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Mutation Strategy", "MS",
            "The mutation strategy. Default: RandomMutation with rate 0.03.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Termination Strategy", "TS",
            "The termination strategy. Default: PopulationDiversity below 2.", GH_ParamAccess.item);

        pManager[3].Optional = true;
        pManager[4].Optional = true;
        pManager[5].Optional = true;
        pManager[6].Optional = true;
        pManager[7].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S",
            "Multi-objective settings for NSGA-II or NSGA-III.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double populationSize = 0;
        double maxGenerations = 0;
        double eliteSize = 0;

        GH_ObjectWrapper selectionStrategyContainer = null;
        GH_ObjectWrapper pairingStrategyContainer = null;
        GH_ObjectWrapper crossoverStrategyContainer = null;
        GH_ObjectWrapper mutationStrategyContainer = null;
        GH_ObjectWrapper terminationStrategyContainer = null;

        if (!DA.GetData(0, ref populationSize)) return;
        if (!DA.GetData(1, ref maxGenerations)) return;
        if (!DA.GetData(2, ref eliteSize)) return;
        DA.GetData(3, ref selectionStrategyContainer);
        DA.GetData(4, ref pairingStrategyContainer);
        DA.GetData(5, ref crossoverStrategyContainer);
        DA.GetData(6, ref mutationStrategyContainer);
        DA.GetData(7, ref terminationStrategyContainer);

        if (populationSize <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Population size must be greater than 0");
            return;
        }
        if (maxGenerations <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Max generations must be greater than 0");
            return;
        }
        if (eliteSize < 0 || eliteSize >= populationSize)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"Elite size must be between 0 and population size - 1 (got {(int)eliteSize}, population {(int)populationSize})");
            return;
        }

        var selectionStrategy = selectionStrategyContainer?.Value as ISelectionStrategy ?? new TournamentSelection(3);
        var pairingStrategy = pairingStrategyContainer?.Value as IPairingStrategy ?? new RankAwarePairingStrategy();
        var crossoverStrategy = crossoverStrategyContainer?.Value as ICrossoverStrategy ?? new TwoPointCrossover(0.7);
        var mutationStrategy = mutationStrategyContainer?.Value as IMutationStrategy ?? new RandomMutation(0.03);
        var terminationStrategy =
            terminationStrategyContainer?.Value as ITerminationStrategy ?? new PopulationDiversity(2);

        if (pairingStrategy is ISingleObjectiveStrategy)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"{pairingStrategy.GetType().Name} is only valid for single-objective solvers. " +
                "Use Rank Aware Pairing or Reference Point Pairing with NSGA-II/III.");
            return;
        }

        if (pairingStrategy is not IMultiObjectiveStrategy)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "Inbreeding Pairing ignores Pareto rank and crowding distance. " +
                "Consider using Rank Aware Pairing or Reference Point Pairing for better multi-objective diversity.");

        var settings = new SolverSettings
        {
            PopulationSize = Convert.ToInt32(populationSize),
            MaxGenerations = Convert.ToInt32(maxGenerations),
            EliteSize = Convert.ToInt32(eliteSize),
            SelectionStrategy = selectionStrategy,
            PairingStrategy = pairingStrategy,
            CrossoverStrategy = crossoverStrategy,
            MutationStrategy = mutationStrategy,
            TerminationStrategy = terminationStrategy,
            IsMultiObjective = true
        };

        DA.SetData(0, settings);
    }
}
