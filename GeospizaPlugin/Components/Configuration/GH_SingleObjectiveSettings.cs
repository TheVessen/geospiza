using System;
using System.Drawing;
using GeospizaCore.Solvers;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Configuration;

public class GH_Settings : GH_Component
{
    public GH_Settings()
        : base("Single-Objective Settings", "SOSettings",
            "Configure parameters and strategies for the single-objective evolutionary solver.",
            "Geospiza", "Configuration")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.Settings;

    public override Guid ComponentGuid => new("7D078EE7-895C-4A27-8EBB-B61A5DC514DF");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Population Size", "PS", "The size of the population", GH_ParamAccess.item, 50);
        pManager.AddNumberParameter("Max Generations", "MG", "The maximum number of generations", GH_ParamAccess.item,
            50);
        pManager.AddNumberParameter("Elite Size", "ES", "The number of elite individuals. If 0 no elite will be picked",
            GH_ParamAccess.item, 1);
        pManager.AddGenericParameter("Selection Strategy", "SS",
            "The selection strategy. As default TournamentSelection with size 3 is used", GH_ParamAccess.item);
        pManager.AddGenericParameter("Pairing Strategy", "PA",
            "The pairing strategy. As default an InBreedingFactor of 0.2 and Manhattan distance will be used",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("Crossover Strategy", "CS",
            "The crossover strategy. As default TwoPoint crossover will be used with a crossover rate of 0.7",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("Mutation Strategy", "MS",
            "The mutation strategy. As default random mutation will be used with a mutation rate of 0.03",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("Termination Strategy", "TS",
            "The termination strategy. As a default it will terminate if the population diversity falls below 2",
            GH_ParamAccess.item);

        pManager[3].Optional = true;
        pManager[4].Optional = true;
        pManager[5].Optional = true;
        pManager[6].Optional = true;
        pManager[7].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
            GH_ParamAccess.item);
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
        var pairingStrategy = pairingStrategyContainer?.Value as IPairingStrategy ?? new PairingStrategy(0.2);
        var crossoverStrategy = crossoverStrategyContainer?.Value as ICrossoverStrategy ?? new TwoPointCrossover(0.7);
        var mutationStrategy = mutationStrategyContainer?.Value as IMutationStrategy ?? new RandomMutation(0.03);
        var terminationStrategy = terminationStrategyContainer?.Value as ITerminationStrategy ?? new PopulationDiversity(2);

        if (pairingStrategy is IMultiObjectiveStrategy)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"{pairingStrategy.GetType().Name} is only valid for multi-objective solvers. " +
                "Use the Multi-Objective Settings component with NSGA-II or NSGA-III, or switch to Inbreeding Pairing.");
            return;
        }

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
            IsMultiObjective = false
        };

        DA.SetData(0, settings);
    }
}