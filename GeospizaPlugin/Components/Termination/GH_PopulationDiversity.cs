using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Termination;

public class GH_PopulationDiversity : GH_Component
{
    /// <summary>
    ///     Initializes a new instance of the GH_PopulationDiversity class.
    /// </summary>
    public GH_PopulationDiversity()
        : base("Population Diversity", "PD",
            "Terminate the genetic algorithm when the population diversity is below a certain threshold",
            "Geospiza", "Termination Strategies")
    {
    }

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.PopulationDiversity;

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("636C470F-C794-4409-8347-B90D64166F74");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Threshold", "T", "The number of unique individuals needed for termination",
            GH_ParamAccess.item, 2);
    }

    /// <summary>
    ///     Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Termination Strategy", "TS", "The termination strategy", GH_ParamAccess.item);
    }

    /// <summary>
    ///     This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double threshold = 0;
        if (!DA.GetData(0, ref threshold)) return;
        DA.SetData(0, new PopulationDiversity(threshold));
    }
}