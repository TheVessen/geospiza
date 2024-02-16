using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Algorythm;
using Geospiza.Strategies;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class Settings : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the Settings class.
    /// </summary>
    public Settings()
        : base("Settings", "S",
            "Settings for the evolutionary algorithm. ",
            "Geospiza", "Solver")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("PopulationSize", "PS", "The size of the population", GH_ParamAccess.item);
        pManager.AddNumberParameter("MaxGenerations", "MG", "The maximum number of generations", GH_ParamAccess.item);
        pManager.AddNumberParameter("EliteSize", "ES", "The number of elite individuals", GH_ParamAccess.item);
        pManager.AddNumberParameter("MutationRate", "MR", "The mutation rate", GH_ParamAccess.item);
        pManager.AddNumberParameter("CrossoverRate", "CR", "The crossover rate", GH_ParamAccess.item);
        pManager.AddGenericParameter("SelectionStrategy", "SS", "The selection strategy", GH_ParamAccess.item);
        pManager.AddGenericParameter("PairingStrategy", "PS", "The pairing strategy", GH_ParamAccess.item);
        pManager.AddGenericParameter("CrossoverStrategy", "CS", "The crossover strategy", GH_ParamAccess.item);
        pManager.AddGenericParameter("MutationStrategy", "MS", "The mutation strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int populationSize = 0;
        int maxGenerations = 0;
        int eliteSize = 0;
        double mutationRate = 0;
        double crossoverRate = 0;
        
        GH_ObjectWrapper selectionStrategyContainer = null;
        GH_ObjectWrapper pairingStrategyContainer = null;
        GH_ObjectWrapper crossoverStrategyContainer = null;
        GH_ObjectWrapper mutationStrategyContainer = null;
        
        ISelectionStrategy selectionStrategy = null;
        IPairingStrategy pairingStrategy = null;
        ICrossoverStrategy crossoverStrategy = null;
        IMutationStrategy mutationStrategy = null;
        
        if (!DA.GetData(0, ref populationSize)) return;
        if (!DA.GetData(1, ref maxGenerations)) return;
        if (!DA.GetData(2, ref eliteSize)) return;
        if (!DA.GetData(3, ref mutationRate)) return;
        if (!DA.GetData(4, ref crossoverRate)) return;
        if (!DA.GetData(5, ref selectionStrategyContainer)) return;
        if (!DA.GetData(6, ref pairingStrategyContainer)) return;
        if (!DA.GetData(7, ref crossoverStrategyContainer)) return;
        if (!DA.GetData(8, ref mutationStrategyContainer)) return;
        
        selectionStrategy = selectionStrategyContainer.Value as ISelectionStrategy;
        pairingStrategy = pairingStrategyContainer.Value as IPairingStrategy;
        crossoverStrategy = crossoverStrategyContainer.Value as ICrossoverStrategy;
        mutationStrategy = mutationStrategyContainer.Value as IMutationStrategy;
        
        var settings = new EvolutionaryAlgorithmSettings
        {
            PopulationSize = populationSize,
            MaxGenerations = maxGenerations,
            EliteSize = eliteSize,
            MutationRate = mutationRate,
            CrossoverRate = crossoverRate,
            SelectionStrategy = selectionStrategy,
            PairingStrategy = pairingStrategy,
            CrossoverStrategy = crossoverStrategy,
            MutationStrategy = mutationStrategy
        };
        
        DA.SetData(0, settings);
        
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon
    {
        get
        {
            //You can add image files to your project resources and access them like this:
            // return Resources.IconForThisComponent;
            return null;
        }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("7D078EE7-895C-4A27-8EBB-B61A5DC514DF"); }
    }
}