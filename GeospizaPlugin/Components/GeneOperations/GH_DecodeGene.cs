using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.GeneOperations;

public class GH_DecodeGene : GH_Component
{
    public GH_DecodeGene()
        : base("Decode Gene", "DG",
            "Decodes a gene into its name, value and unique identifier",
            "Geospiza", "GeneOperations")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap Icon => Resources.DeconstructGene;

    public override Guid ComponentGuid => new("BAE4D73B-B25C-4A95-95D2-36BAED583297");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Gene", "G", "The gene to decode", GH_ParamAccess.list);
        pManager.AddGenericParameter("Observer", "O",
            "The EvolutionObserver from the solver. When connected, outputs actual slider values instead of raw tick indices.",
            GH_ParamAccess.item);
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Gene Name", "GN", "The name of the gene", GH_ParamAccess.list);
        pManager.AddNumberParameter("Gene Value", "GV", "The value of the gene", GH_ParamAccess.list);
        pManager.AddTextParameter("Gene Guid", "GG", "The guid of the gene", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var genes = new List<Gene>();
        DA.GetDataList(0, genes);

        GH_ObjectWrapper obsWrapper = null;
        DA.GetData(1, ref obsWrapper);
        var obs = obsWrapper?.Value as EvolutionObserver;

        // Build a guid→schema lookup once if observer is available
        Dictionary<Guid, GeneSchema> schemaLookup = null;
        if (obs?.GeneSchema != null)
            schemaLookup = obs.GeneSchema.ToDictionary(s => s.GeneGuid);

        var names  = new List<string>();
        var values = new List<double>();
        var guids  = new List<string>();

        foreach (var gene in genes)
        {
            if (gene == null) continue;

            var name = gene.GeneName;
            if (gene.GenePoolIndex != -1) name = name + "_" + gene.GenePoolIndex;
            names.Add(name);
            guids.Add(gene.GhInstanceGuid.ToString());

            double value = gene.TickValue;
            if (schemaLookup != null && schemaLookup.TryGetValue(gene.GeneGuid, out var schema))
            {
                var real = schema.TickToValue(gene.TickValue);
                if (!double.IsNaN(real)) value = real;
            }
            values.Add(value);
        }

        DA.SetDataList(0, names);
        DA.SetDataList(1, values);
        DA.SetDataList(2, guids);
    }
}
