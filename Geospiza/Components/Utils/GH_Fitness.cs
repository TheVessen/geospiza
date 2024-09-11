using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaManager.Core;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class GH_Fitness : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the Fitness class.
    /// </summary>
    public GH_Fitness()
        : base("Fitness", "F",
            "Fitness value for the evolutionary algorithm. This component links the fitness value to the main solver",
            "Geospiza", "Utils")
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
    
    public double FitnessValue { get; set; } = 0;

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        
        double fitness = 0;
        if (!DA.GetData(0, ref fitness)) return;

        // Add the new fitness value
        Fitness.Instance.SetFitness(fitness);


        
    }
    
    public override GH_Exposure Exposure => GH_Exposure.primary;
 
    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.Fitness;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("FC2E37D7-CE42-4232-B1C8-07C81ADF75D7"); }
    }
}