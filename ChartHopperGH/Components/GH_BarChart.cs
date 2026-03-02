using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper.Kernel;

namespace ChartHopperGH.Components;

public class GH_BarChart : GH_Component
{
    public GH_BarChart()
        : base("Bar Chart", "Bar",
            "Creates an interactive Plotly bar chart as HTML",
            "ChartHopper", "Charts")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-3333-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Categories", "C", "Category labels", GH_ParamAccess.list);
        pManager.AddNumberParameter("Values", "V", "Values", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Horizontal", "H", "Horizontal bars", GH_ParamAccess.item, false);
        pManager.AddTextParameter("Title", "T", "Chart title", GH_ParamAccess.item, "Bar Chart");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Standalone HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var categories = new List<string>();
        var values = new List<double>();
        var horizontal = false;
        var title = "";

        if (!DA.GetDataList(0, categories)) return;
        if (!DA.GetDataList(1, values)) return;
        DA.GetData(2, ref horizontal);
        DA.GetData(3, ref title);

        var builder = new ChartBuilder(ChartType.Bar)
            .Title(title)
            .Categories(categories, values)
            .Height(Math.Max(300, categories.Count * 35 + 80));

        if (horizontal) builder.Horizontal();

        DA.SetData(0, builder.Build());
    }
}
