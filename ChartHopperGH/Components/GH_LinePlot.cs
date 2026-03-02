using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace ChartHopperGH.Components;

public class GH_LinePlot : GH_Component
{
    public GH_LinePlot()
        : base("Line Plot", "Line",
            "Creates an interactive Plotly line chart as HTML",
            "ChartHopper", "Charts")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-2222-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("X", "X", "X values (shared across all traces)", GH_ParamAccess.list);
        pManager.AddNumberParameter("Y", "Y", "Y values (tree: one branch per trace)", GH_ParamAccess.tree);
        pManager.AddTextParameter("Names", "N", "Trace names", GH_ParamAccess.list);
        pManager.AddTextParameter("Title", "T", "Chart title", GH_ParamAccess.item, "Line Plot");
        pManager.AddTextParameter("X Label", "XL", "X axis label", GH_ParamAccess.item, "");
        pManager.AddTextParameter("Y Label", "YL", "Y axis label", GH_ParamAccess.item, "");
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Standalone HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var x = new List<double>();
        var names = new List<string>();
        var title = "";
        var xLabel = "";
        var yLabel = "";

        if (!DA.GetDataList(0, x)) return;
        if (!DA.GetDataTree(1, out GH_Structure<Grasshopper.Kernel.Types.GH_Number> yTree)) return;
        DA.GetDataList(2, names);
        DA.GetData(3, ref title);
        DA.GetData(4, ref xLabel);
        DA.GetData(5, ref yLabel);

        var builder = new ChartBuilder(ChartType.Line)
            .Title(title)
            .Height(400);

        if (!string.IsNullOrEmpty(xLabel)) builder.XAxis(xLabel);
        if (!string.IsNullOrEmpty(yLabel)) builder.YAxis(yLabel);

        for (var i = 0; i < yTree.Branches.Count; i++)
        {
            var branch = yTree.Branches[i];
            var yValues = new List<double>();
            foreach (var num in branch)
                yValues.Add(num.Value);

            var name = i < names.Count ? names[i] : $"Series {i + 1}";
            builder.AddTrace(x, yValues, name);
        }

        DA.SetData(0, builder.Build());
    }
}
