using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.GeneOperations;

public class GH_AutoGeneCollector : GH_Component
{
    private readonly List<string> docParams = new();


    public GH_AutoGeneCollector()
        : base("Auto Gene Collector", "AGC",
            "Automatically finds and collects gene parameters from Gene Pool components and sliders prefixed with 'GP_'",
            "Geospiza", "GeneOperations")
    {
    }


    protected override Bitmap Icon => null;


    public override Guid ComponentGuid => new("CC1BA854-CDE4-4A88-BFE7-97105DD75F9B");


    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("Clear", "C", "Clear the gene parameters from the doc search", GH_ParamAccess.item,
            false);
        pManager.AddBooleanParameter("Search Document", "SD", "Search the document for gene parameters",
            GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Genes", "GID", "The gene ids", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var geneIds = new List<string>();
        var searchDocument = false;
        DA.GetData(1, ref searchDocument);
        var clear = false;
        DA.GetData(0, ref clear);


        if (searchDocument)
            foreach (var ghobject in OnPingDocument().Objects)
            {
                var currentType = ghobject.GetType().ToString();

                var isRightType = currentType == "Grasshopper.Kernel.Special.GH_NumberSlider" ||
                                  currentType == "GalapagosComponents.GalapagosGeneListObject";
                if (ghobject.NickName.StartsWith("GP_") && isRightType)
                    if (!docParams.Contains(ghobject.InstanceGuid.ToString()))
                        docParams.Add(ghobject.InstanceGuid.ToString());
            }

        if (clear) docParams.Clear();

        geneIds.AddRange(docParams);
        DA.SetDataList(0, geneIds);
    }
}