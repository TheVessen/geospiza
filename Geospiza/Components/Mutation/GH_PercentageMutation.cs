using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Mutation;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents.Mutation;

public class GH_PercentageMutation : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the PercentageMutation class.
    /// </summary>
    public GH_PercentageMutation()
        : base("PercentageMutation", "PM",
            "Performs a percentage mutation",
            "Geospiza", "MutationStrategies")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("MutationRate", "MR", "The mutation rate", GH_ParamAccess.item, 0.01);
        pManager.AddNumberParameter("Percentage", "P", "The percentage of the population to mutate", GH_ParamAccess.item, 0.1);
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
        double percentage = 0;
        if (!DA.GetData(0, ref mutationRate)) return;
        if (!DA.GetData(1, ref percentage)) return;
        
        var strategy = new PercentageMutation(mutationRate, percentage);
        
        DA.SetData(0, strategy);
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.PercentageMutation;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("7BFDF3C6-F4FB-4F26-BDC2-C76E20FE95B8"); }
    }
}