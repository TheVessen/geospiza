using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.GeneOperations;

public class GH_GeneCollector : GH_Component
{
    private List<string> docParams = new();
    //REF "https://discourse.mcneel.com/t/gene-pool-component/59835/8"

    public GH_GeneCollector()
        : base("Gene Collector", "GC",
            "Collects and identifies parameters for evolutionary optimization. Compatible with numeric sliders and Galapagos gene pools.",
            "Geospiza", "GeneOperations")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;


    protected override Bitmap Icon => Resources.GeneSelector;


    public override Guid ComponentGuid => new("DCCF2B6C-6790-4610-821B-F26C2FC938C2");


    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Gene Params", "GP",
            "The gene parameters this can be number sliders or a galapagos gene list", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Genes", "GID", "The gene ids", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var allGeneParams = Params.Input[0].Sources;
        var geneIds = new List<string>();

        foreach (var param in allGeneParams)
            geneIds.Add(param.InstanceGuid.ToString());

        if (geneIds.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "No gene parameters connected. Connect number sliders or gene pools.");
            return;
        }

        Message = $"{geneIds.Count} gene{(geneIds.Count == 1 ? "" : "s")}";
        DA.SetDataList(0, geneIds);
    }
}