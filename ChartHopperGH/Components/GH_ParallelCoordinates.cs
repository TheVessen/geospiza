using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace ChartHopperGH.Components;

public class GH_ParallelCoordinates : GH_Component
{
    public GH_ParallelCoordinates()
        : base("Parallel Coordinates", "ParCoords",
            "Creates an interactive Plotly parallel coordinates plot as HTML",
            "ChartHopper", "Charts")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-5555-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Data", "D", "Dimension data (tree: one branch per dimension)",
            GH_ParamAccess.tree);
        pManager.AddTextParameter("Labels", "L", "Dimension labels", GH_ParamAccess.list);
        pManager.AddNumberParameter("Color", "C", "Color values", GH_ParamAccess.list);
        pManager.AddTextParameter("Title", "T", "Chart title", GH_ParamAccess.item, "Parallel Coordinates");
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Standalone HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var labels = new List<string>();
        var color = new List<double>();
        var title = "";

        if (!DA.GetDataTree(0, out GH_Structure<Grasshopper.Kernel.Types.GH_Number> dataTree)) return;
        if (!DA.GetDataList(1, labels)) return;
        DA.GetDataList(2, color);
        DA.GetData(3, ref title);

        var builder = new ChartBuilder(ChartType.Parcoords)
            .Title(title)
            .Height(450);

        for (var i = 0; i < dataTree.Branches.Count; i++)
        {
            var branch = dataTree.Branches[i];
            var values = new List<double>();
            foreach (var num in branch)
                values.Add(num.Value);

            var label = i < labels.Count ? labels[i] : $"Dim {i + 1}";
            builder.AddDimension(label, values);
        }

        if (color.Count > 0) builder.Color(color, colorBarTitle: "Value");

        DA.SetData(0, builder.Build());
    }
}
