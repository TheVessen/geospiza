using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Converters;

public class GH_ObserverToJson : GH_Component
{
    public GH_ObserverToJson()
        : base("Observer To Json", "OToJ",
            "Converts an observer to a JSON string",
            "Geospiza", "Converters")
    {
    }

    protected override Bitmap Icon =>
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        null;

    public override Guid ComponentGuid => new("D5FA6C30-850C-4188-BDB2-A3CC7B971275");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The observer to convert to JSON", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The observer as a JSON string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Get inputs
        GH_ObjectWrapper observer = null;
        if (!DA.GetData(0, ref observer)) return;
        var observerType = observer.ScriptVariable() as EvolutionObserver;

        // Convert the observer to a JSON string
        var json = observerType.ToJson();

        // Set the output
        DA.SetData(0, json);
    }
}