using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Converters;

public class GH_IndividualFromJson : GH_Component
{
    public GH_IndividualFromJson()
        : base("Individual From Json", "IFJ",
            "Converts a JSON string to an individual",
            "Geospiza", "Converters")
    {
    }

    protected override Bitmap Icon =>
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        null;

    public override Guid ComponentGuid => new("7DAE3EE7-F6F1-4F73-BA81-753B2ABBB617");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Individual", "I", "The individual", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var json = "";
        if (!DA.GetData(0, ref json)) return;

        if (json == "") return;

        var individual = Individual.FromJson(json);

        DA.SetData(0, individual);
    }
}