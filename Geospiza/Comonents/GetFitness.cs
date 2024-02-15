using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class GetFitness : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the CLASS_NAME class.
    /// </summary>
    public GetFitness()
        : base("GetFitness", "GF",
            "Description",
            "Geospiza", "Utils")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("Fitness", "F", "The fitness value", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var obj = this.OnPingDocument().Objects;

        double fitnessValue = 0;
        
        foreach (var comp in obj)
        {
            if (comp is Fitness fitness)
            {
                
                fitnessValue = fitness.FitnessValue;
            }
        }
        
        DA.SetData(0, fitnessValue);
        
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
        get { return new Guid("484E527A-F539-4A9A-AC03-D66915738018"); }
    }
}