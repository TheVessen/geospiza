using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class Fitness : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the Fitness class.
    /// </summary>
    public Fitness()
        : base("Fitness", "F",
            "Fitness value for the evolutionary algorithm. ",
            "Geospiza", "Subcategory")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Fitness", "F", "Fitness value for the evolutionary algorithm.", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }
    
    public static double FitnessValue { get; set; }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        
        double fitness = 0;
        if (!DA.GetData(0, ref fitness)) return;
        FitnessValue = fitness;
        
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
        get { return new Guid("FC2E37D7-CE42-4232-B1C8-07C81ADF75D7"); }
    }
}