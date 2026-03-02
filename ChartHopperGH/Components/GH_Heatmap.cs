using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace ChartHopperGH.Components;

public class GH_Heatmap : GH_Component
{
    public GH_Heatmap()
        : base("Heatmap", "Heat",
            "Creates an interactive Plotly heatmap as HTML",
            "ChartHopper", "Charts")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-6666-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Matrix", "M", "Matrix data (tree: one branch per row)",
            GH_ParamAccess.tree);
        pManager.AddTextParameter("Row Labels", "RL", "Row labels", GH_ParamAccess.list);
        pManager.AddTextParameter("Column Labels", "CL", "Column labels", GH_ParamAccess.list);
        pManager.AddTextParameter("Title", "T", "Chart title", GH_ParamAccess.item, "Heatmap");
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Standalone HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var rowLabels = new List<string>();
        var colLabels = new List<string>();
        var title = "";

        if (!DA.GetDataTree(0, out GH_Structure<Grasshopper.Kernel.Types.GH_Number> matrixTree)) return;
        DA.GetDataList(1, rowLabels);
        DA.GetDataList(2, colLabels);
        DA.GetData(3, ref title);

        var z = new double[matrixTree.Branches.Count][];
        for (var i = 0; i < matrixTree.Branches.Count; i++)
        {
            var branch = matrixTree.Branches[i];
            z[i] = new double[branch.Count];
            for (var j = 0; j < branch.Count; j++)
                z[i][j] = branch[j].Value;
        }

        var builder = new ChartBuilder(ChartType.Heatmap)
            .Title(title)
            .Matrix(z,
                colLabels.Count > 0 ? colLabels : null,
                rowLabels.Count > 0 ? rowLabels : null)
            .Height(400);

        DA.SetData(0, builder.Build());
    }
}
