using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaManager.Strategies;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza;

public class GH_PopulationDiversity : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_PopulationDiversity class.
    /// </summary>
    public GH_PopulationDiversity()
        : base("PopulationDiversity", "PD",
            "Terminate the genetic algorithm when the population diversity is below a certain threshold",
            "Geospiza", "TerminationStrategies")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Threshold", "T", "The number of unique individuals needed for termination", GH_ParamAccess.item, 2);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("TerminationStrategy", "TS", "The termination strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double threshold = 0;
        if (!DA.GetData(0, ref threshold)) return;
        DA.SetData(0, new PopulationDiversity(threshold));
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.PopulationDiversity;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("636C470F-C794-4409-8347-B90D64166F74"); }
    }
}

