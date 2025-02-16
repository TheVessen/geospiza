﻿using System;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Utils;

public class GH_Observer : GH_Component
{
    /// <summary>
    ///     Initializes a new instance of the Observer class.
    /// </summary>
    public GH_Observer()
        : base("Observer", "Observer",
            "Monitors and displays statistics from evolutionary algorithms including fitness metrics and population data",
            "Geospiza", "Utils")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.Observer;

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("2D368D5B-DC50-432D-85DD-311435EF865C");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "P", "The population to observe", GH_ParamAccess.item);
    }

    /// <summary>
    ///     Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I", "All individual from the last generation",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Average Fitness", "AF", "Average fitness for each generation",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Best Fitness", "BF", "Best fitness for each generation", GH_ParamAccess.list);
        pManager.AddNumberParameter("Worst Fitness", "WF", "Worst fitness for each generation", GH_ParamAccess.list);
        pManager.AddGenericParameter("Best Individual", "BI", "The 5 best individuals over all generations",
            GH_ParamAccess.list);
    }

    /// <summary>
    ///     This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper wrapper = null;

        if (!DA.GetData(0, ref wrapper)) return;

        if (wrapper.Value is EvolutionObserver)
        {
            var obs = (EvolutionObserver)wrapper.Value;
            if (obs.CurrentPopulation == null || obs.CurrentPopulation.Count == 0) return;

            DA.SetDataList(0, obs.CurrentPopulation.Inhabitants);
            DA.SetDataList(1, obs.AverageFitness);
            DA.SetDataList(2, obs.BestFitness);
            DA.SetDataList(3, obs.WorstFitness);
            DA.SetDataList(4, obs.BestIndividuals);
        }
        else
        {
            throw new Exception("The input is not a population");
        }
    }
}