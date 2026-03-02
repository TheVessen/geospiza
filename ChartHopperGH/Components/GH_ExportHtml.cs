using System;
using System.Drawing;
using ChartHopper;
using Grasshopper.Kernel;

namespace ChartHopperGH.Components;

public class GH_ExportHtml : GH_Component
{
    public GH_ExportHtml()
        : base("Export HTML", "Export",
            "Saves HTML chart to a file and optionally opens it in the browser",
            "ChartHopper", "Layout")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-8888-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "HTML string to export", GH_ParamAccess.item);
        pManager.AddTextParameter("Directory", "D", "Output directory (defaults to Desktop)",
            GH_ParamAccess.item);
        pManager.AddTextParameter("FileName", "F", "File name without extension", GH_ParamAccess.item, "chart");
        pManager.AddBooleanParameter("Open", "O", "Open in browser", GH_ParamAccess.item, true);
        pManager.AddBooleanParameter("Export", "E", "Trigger export", GH_ParamAccess.item, false);
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("FilePath", "P", "Path to the saved HTML file", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var export = false;
        if (!DA.GetData(4, ref export) || !export) return;

        var html = "";
        if (!DA.GetData(0, ref html)) return;

        var directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var tempDir = "";
        if (DA.GetData(1, ref tempDir) && !string.IsNullOrWhiteSpace(tempDir))
            directory = tempDir;

        var fileName = "chart";
        var tempName = "";
        if (DA.GetData(2, ref tempName) && !string.IsNullOrWhiteSpace(tempName))
            fileName = tempName;

        var openInBrowser = true;
        DA.GetData(3, ref openInBrowser);

        try
        {
            var filePath = HtmlExporter.Export(html, directory, fileName, openInBrowser);
            DA.SetData(0, filePath);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Export failed: {ex.Message}");
        }
    }
}
