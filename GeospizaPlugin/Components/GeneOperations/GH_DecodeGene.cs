using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

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

        var names = new List<string>();
        var values = new List<double>();
        var guid = new List<string>();

        foreach (var gene in genes)
        {
            if (gene == null) continue;
            var name = gene.GeneName;
            if (gene.GenePoolIndex != -1) name = name + "_" + gene.GenePoolIndex;
            names.Add(name);
            values.Add(gene.TickValue);
            guid.Add(gene.GhInstanceGuid.ToString());
        }

        DA.SetDataList(0, names);
        DA.SetDataList(1, values);
        DA.SetDataList(2, guid);
    }
}