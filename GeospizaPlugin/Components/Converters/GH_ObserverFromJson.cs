using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Converters;

public class GH_ObserverFromJson : GH_Component
{
    public GH_ObserverFromJson()
        : base("Observer From Json", "OFJ",
            "Converts a JSON string to an observer",
            "Geospiza", "Converters")
    {
    }

    protected override Bitmap Icon =>
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        null;

    public override Guid ComponentGuid => new("DC2277B8-3EBF-488E-86DA-8056B881A483");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The observer", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Declare a variable for the input
        var json = "";
        // If the input is not retrieved, return
        if (!DA.GetData(0, ref json)) return;

        if (json == "") return;

        var observer = EvolutionObserver.FromJson(json);

        DA.SetData(0, observer);
    }
}