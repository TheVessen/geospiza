using System;
using System.Drawing;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Webcomponents;

public class GH_AditionalData : GH_Component
{
    /// <summary>
    ///     Initializes a new instance of the GH_AditionalData class.
    /// </summary>
    public GH_AditionalData()
        : base("Additional Data", "Additional Data",
            "Add additional data to the web output",
            "Geospiza", "Webcomponents")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.AdditionalData;

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("85B91440-7C06-4923-A8BC-4A0D0C4DDF9C");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "The key of the data", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "The value of the data", GH_ParamAccess.item);
    }

    /// <summary>
    ///     Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Additional Data", "AD", "The additional data", GH_ParamAccess.item);
    }

    /// <summary>
    ///     This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var key = "";
        if (!DA.GetData(0, ref key)) return;
        var value = "";
        if (!DA.GetData(1, ref value)) return;
        var additionalData = new Tuple<string, string>(key, value);

        DA.SetData(0, additionalData);
    }
}