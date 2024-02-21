using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Mutation;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Components.Mutation;

public class GH_RandomMutation : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_RandomMutation class.
    /// </summary>
    public GH_RandomMutation()
        : base("RandomMutation", "RM",
            "Performs a random mutation",
            "Geospiza", "MutationStrategies")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("MutationRate", "MR", "The mutation rate", GH_ParamAccess.item, 0.01);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("MutationStrategy", "MS", "The mutation strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double mutationRate = 0;
        if (!DA.GetData(0, ref mutationRate)) return;

        var strategy = new RandomMutation(mutationRate);

        DA.SetData(0, strategy);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.RandomMutation;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("10050646-9E96-4EB4-82F8-10731E21DD23"); }
    }
}