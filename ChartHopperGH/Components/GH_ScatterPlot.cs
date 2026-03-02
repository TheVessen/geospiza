using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper.Kernel;

namespace ChartHopperGH.Components;

public class GH_ScatterPlot : GH_Component
{
    public GH_ScatterPlot()
        : base("Scatter Plot", "Scatter",
            "Creates an interactive Plotly scatter plot as HTML",
            "ChartHopper", "Charts")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-1111-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("X", "X", "X values", GH_ParamAccess.list);
        pManager.AddNumberParameter("Y", "Y", "Y values", GH_ParamAccess.list);
        pManager.AddNumberParameter("Color", "C", "Color values (numeric)", GH_ParamAccess.list);
        pManager.AddTextParameter("Title", "T", "Chart title", GH_ParamAccess.item, "Scatter Plot");
        pManager.AddTextParameter("X Label", "XL", "X axis label", GH_ParamAccess.item, "");
        pManager.AddTextParameter("Y Label", "YL", "Y axis label", GH_ParamAccess.item, "");
        pManager.AddBooleanParameter("WebGL", "GL", "Use WebGL for large datasets", GH_ParamAccess.item, false);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Standalone HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var x = new List<double>();
        var y = new List<double>();
        var color = new List<double>();
        var title = "";
        var xLabel = "";
        var yLabel = "";
        var webgl = false;

        if (!DA.GetDataList(0, x)) return;
        if (!DA.GetDataList(1, y)) return;
        DA.GetDataList(2, color);
        DA.GetData(3, ref title);
        DA.GetData(4, ref xLabel);
        DA.GetData(5, ref yLabel);
        DA.GetData(6, ref webgl);

        var builder = new ChartBuilder(ChartType.Scatter)
            .Title(title)
            .Data(x, y)
            .Height(400);

        if (webgl) builder.UseWebGL();
        if (!string.IsNullOrEmpty(xLabel)) builder.XAxis(xLabel);
        if (!string.IsNullOrEmpty(yLabel)) builder.YAxis(yLabel);
        if (color.Count > 0) builder.Color(color);

        DA.SetData(0, builder.Build());
    }
}
