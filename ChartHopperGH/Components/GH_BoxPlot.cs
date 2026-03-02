using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace ChartHopperGH.Components;

public class GH_BoxPlot : GH_Component
{
    public GH_BoxPlot()
        : base("Box Plot", "Box",
            "Creates an interactive Plotly box plot as HTML",
            "ChartHopper", "Charts")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-4444-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Data", "D", "Data groups (tree: one branch per group)", GH_ParamAccess.tree);
        pManager.AddTextParameter("Names", "N", "Group names", GH_ParamAccess.list);
        pManager.AddTextParameter("Title", "T", "Chart title", GH_ParamAccess.item, "Box Plot");
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Standalone HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var names = new List<string>();
        var title = "";

        if (!DA.GetDataTree(0, out GH_Structure<Grasshopper.Kernel.Types.GH_Number> dataTree)) return;
        DA.GetDataList(1, names);
        DA.GetData(2, ref title);

        var builder = new ChartBuilder(ChartType.Box)
            .Title(title)
            .Height(400);

        for (var i = 0; i < dataTree.Branches.Count; i++)
        {
            var branch = dataTree.Branches[i];
            var data = new List<double>();
            foreach (var num in branch)
                data.Add(num.Value);

            var name = i < names.Count ? names[i] : $"Group {i + 1}";
            builder.AddGroup(name, data);
        }

        DA.SetData(0, builder.Build());
    }
}
