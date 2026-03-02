using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Converters;

public class IndividualToJson : GH_Component
{
    public IndividualToJson()
        : base("Individual To Json", "IToJ",
            "Converts individuals to JSON strings",
            "Geospiza", "Converters")
    {
    }

    protected override Bitmap Icon => Resources.IndividualToJSON;

    public override Guid ComponentGuid => new("FDB78846-7982-42E4-B8ED-EF37AC136612");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I", "The individuals to convert to JSON",
            GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON strings", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var wrappers = new List<GH_ObjectWrapper>();
        if (!DA.GetDataList(0, wrappers)) return;

        var results = wrappers
            .Select(w => w.Value)
            .OfType<Individual>()
            .Select(ind => ind.ToJson())
            .ToList();

        DA.SetDataList(0, results);
    }
}
