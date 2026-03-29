using System;
using System.Drawing;
using GeospizaCore.Solvers;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Converters;

public class GH_SettingFromJson : GH_Component
{
    public GH_SettingFromJson()
        : base("Setting From Json", "SFJ",
            "Converts a JSON string to a setting",
            "Geospiza", "Converters")
    {
    }

    protected override Bitmap Icon =>
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        null;

    public override Guid ComponentGuid => new("F975E08F-E01C-41A4-8270-576A00B48997");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Setting", "S", "The setting", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Declare a variable for the input
        var json = "";
        // If the input is not retrieved, return
        if (!DA.GetData(0, ref json)) return;

        if (string.IsNullOrWhiteSpace(json))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "JSON string is empty.");
            return;
        }

        SolverSettings setting;
        try
        {
            setting = SolverSettings.FromJson(json);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to deserialize settings: {ex.Message}");
            return;
        }

        DA.SetData(0, setting);
    }
}