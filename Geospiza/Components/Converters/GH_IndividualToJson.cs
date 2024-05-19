using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class IndividualToJson : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the IndividualToJson class.
    /// </summary>
    public IndividualToJson()
        : base("IndividualToJson", "IndividualToJson",
            "Converts an individual to a JSON string",
            "Geospiza", "Converter")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individual", "I", "The individual to convert to JSON", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }
    

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Declare a variable for the input
        GH_ObjectWrapper individualWrapper = new GH_ObjectWrapper();
        // If the input is not retrieved, return
        if (!DA.GetData(0, ref individualWrapper)) return;

        if (individualWrapper.Value is Individual individual)
        {
            var json = individual.ToJson();
        
            DA.SetData(0, json);
        }
    }
    
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.IndividualToJSON;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("FDB78846-7982-42E4-B8ED-EF37AC136612"); }
    }
}