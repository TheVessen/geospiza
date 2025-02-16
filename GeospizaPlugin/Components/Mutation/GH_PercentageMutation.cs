﻿using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Mutation;

public class GH_PercentageMutation : GH_Component
{
    /// <summary>
    ///     Initializes a new instance of the PercentageMutation class.
    /// </summary>
    public GH_PercentageMutation()
        : base("Percentage Mutation", "PM",
            "The PercentageMutation applies a mutation to each gene in an individual's gene pool. " +
            "It calculates a mutation amount based on the gene's current value and a predefined mutation percentage, " +
            "then adjusts the gene's value within a valid range, introducing variability in the gene pool.",
            "Geospiza", "MutationStrategies")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.PercentageMutation;

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("7BFDF3C6-F4FB-4F26-BDC2-C76E20FE95B8");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Mutation Rate", "MR", "The mutation rate", GH_ParamAccess.item, 0.01);
        pManager.AddNumberParameter("Percentage", "P", "The percentage of the population to mutate eg. 0.1 for 10%",
            GH_ParamAccess.item, 0.1);
    }

    /// <summary>
    ///     Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Mutation Strategy", "MS", "The mutation strategy", GH_ParamAccess.item);
    }

    /// <summary>
    ///     This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double mutationRate = 0;
        double percentage = 0;
        if (!DA.GetData(0, ref mutationRate)) return;
        if (!DA.GetData(1, ref percentage)) return;

        var strategy = new PercentageMutation(mutationRate, percentage);

        DA.SetData(0, strategy);
    }
}