using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Mutation;

public class GH_RandomMutation : GH_Component
{
    /// <summary>
    ///     Initializes a new instance of the GH_RandomMutation class.
    /// </summary>
    public GH_RandomMutation()
        : base("RandomMutation", "RM",
            "Applies a random mutation to each gene in an individual's gene pool, where the new value of the gene is a " +
            "random number within the range of the gene's valid values. This strategy " +
            "introduces variability in the gene pool, aiding in the " +
            "exploration of the solution space.",
            "Geospiza", "MutationStrategies")
    {
    }

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.RandomMutation;

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("10050646-9E96-4EB4-82F8-10731E21DD23");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Mutation Rate", "MR", "The mutation rate", GH_ParamAccess.item, 0.01);
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
        if (!DA.GetData(0, ref mutationRate)) return;

        var strategy = new RandomMutation(mutationRate);

        DA.SetData(0, strategy);
    }
}