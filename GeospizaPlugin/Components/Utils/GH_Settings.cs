using System;
using System.Drawing;
using GeospizaCore.Solvers;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Utils;

public class GH_Settings : GH_Component
{
    /// <summary>
    ///   Initializes a new instance of the Settings class.
    /// </summary>
    public GH_Settings()
        : base("Settings", "Settings",
            "Configure parameters and strategies for the evolutionary algorithm including population size, generations, selection, crossover, and mutation settings.",
            "Geospiza", "Utils")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    /// <summary>
    ///   Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.Settings;

    /// <summary>
    ///   Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("7D078EE7-895C-4A27-8EBB-B61A5DC514DF");

    /// <summary>
    ///   Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Population Size", "PS", "The size of the population", GH_ParamAccess.item, 50);
        pManager.AddNumberParameter("Max Generations", "MG", "The maximum number of generations", GH_ParamAccess.item,
            25);
        pManager.AddNumberParameter("Elite Size", "ES", "The number of elite individuals. If 0 no elite will be picked",
            GH_ParamAccess.item, 0);
        pManager.AddGenericParameter("Selection Strategy", "SS",
            "The selection strategy. As default StochasticUniversalSampling is used", GH_ParamAccess.item);
        pManager.AddGenericParameter("Pairing Strategy", "PS",
            "The pairing strategy. As default an InBreedingFactor of 0.2 and Manhattan distance will be used",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("Crossover Strategy", "CS",
            "The crossover strategy. As default TwoPoint crossover will be used with a crossover rate of 0.6",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("Mutation Strategy", "MS",
            "The mutation strategy. As default random mutation will be used with a mutation rate of 0.01",
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

    /// <summary>
    ///   Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
            GH_ParamAccess.item);
    }

    /// <summary>
    ///   This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double populationSize = 0;
        double maxGenerations = 0;
        double eliteSize = 0;
        double mutationRate = 0;
        double crossoverRate = 0;

        GH_ObjectWrapper selectionStrategyContainer = null;
        GH_ObjectWrapper pairingStrategyContainer = null;
        GH_ObjectWrapper crossoverStrategyContainer = null;
        GH_ObjectWrapper mutationStrategyContainer = null;
        GH_ObjectWrapper terminationStrategyContainer = null;

        ISelectionStrategy selectionStrategy = null;
        PairingStrategy pairingStrategy = null;
        ICrossoverStrategy crossoverStrategy = null;
        IMutationStrategy mutationStrategy = null;
        ITerminationStrategy terminationStrategy = null;

        if (!DA.GetData(0, ref populationSize)) return;
        if (!DA.GetData(1, ref maxGenerations)) return;
        if (!DA.GetData(2, ref eliteSize)) return;
        DA.GetData(3, ref selectionStrategyContainer);
        DA.GetData(4, ref pairingStrategyContainer);
        DA.GetData(5, ref crossoverStrategyContainer);
        DA.GetData(6, ref mutationStrategyContainer);
        DA.GetData(7, ref terminationStrategyContainer);

        selectionStrategy =
            selectionStrategyContainer?.Value as ISelectionStrategy ?? new StochasticUniversalSampling();
        pairingStrategy = pairingStrategyContainer?.Value as PairingStrategy ?? new PairingStrategy(0.2);
        crossoverStrategy = crossoverStrategyContainer?.Value as ICrossoverStrategy ?? new TwoPointCrossover(0.6);
        mutationStrategy = mutationStrategyContainer?.Value as IMutationStrategy ?? new RandomMutation(0.01);
        terminationStrategy = terminationStrategyContainer?.Value as ITerminationStrategy ?? new PopulationDiversity(2);

        if (populationSize <= 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Population size must be greater than 0");
        if (maxGenerations <= 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Max generations must be greater than 0");
        if (mutationRate is < 0 or > 1)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mutation rate must be between 0 and 1");
        if (crossoverRate is < 0 or > 1)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Crossover rate must be between 0 and 1");

        var settings = new SolverSettings
        {
            PopulationSize = Convert.ToInt32(populationSize),
            MaxGenerations = Convert.ToInt32(maxGenerations),
            EliteSize = Convert.ToInt32(eliteSize),
            SelectionStrategy = selectionStrategy,
            PairingStrategy = pairingStrategy,
            CrossoverStrategy = crossoverStrategy,
            MutationStrategy = mutationStrategy,
            TerminationStrategy = terminationStrategy
        };

        DA.SetData(0, settings);
    }
}