using System;
using System.Drawing;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Webcomponents;

public class GH_AditionalData : GH_Component
{
    public GH_AditionalData()
        : base("Additional Data", "Additional Data",
            "Add additional data to the web output",
            "Geospiza", "Webcomponents")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap Icon => Resources.AdditionalData;

    public override Guid ComponentGuid => new("85B91440-7C06-4923-A8BC-4A0D0C4DDF9C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "The key of the data", GH_ParamAccess.item);
        pManager.AddTextParameter("Value", "V", "The value of the data", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Additional Data", "AD", "The additional data", GH_ParamAccess.item);
    }

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