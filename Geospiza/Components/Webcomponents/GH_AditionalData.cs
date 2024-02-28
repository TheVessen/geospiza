using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza;

public class GH_AditionalData : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_AditionalData class.
    /// </summary>
    public GH_AditionalData()
        : base("AditionalData", "AD",
            "Add aditional data to the weboutput",
            "Geospiza", "Webcomponents")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "The key of the data", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "The value of the data", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("AditionalData", "AD", "The aditional data", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string key = "";
        if (!DA.GetData(0, ref key)) return;
        string value = "";
        if (!DA.GetData(1, ref value)) return;
        Tuple<string, string> aditionalData = new Tuple<string, string>(key, value);
        
        DA.SetData(0, aditionalData);
    }
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.AdditionalData;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("85B91440-7C06-4923-A8BC-4A0D0C4DDF9C"); }
    }
}