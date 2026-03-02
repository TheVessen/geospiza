using System;
using System.Drawing;
using GeospizaCore.Solvers;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Converters;

public class GH_SettingToJson : GH_Component
{
    public GH_SettingToJson()
        : base("Setting To Json", "SToJ",
            "Converts a setting to a JSON string",
            "Geospiza", "Converters")
    {
    }

    protected override Bitmap Icon => null;

    public override Guid ComponentGuid => new("B6D9077C-391E-4DB7-B954-0CE7A2C1A333");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Setting", "S", "The setting to convert to JSON", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var settingWrapper = new GH_ObjectWrapper();
        if (!DA.GetData(0, ref settingWrapper)) return;

        if (settingWrapper.Value is SolverSettings setting)
        {
            var json = setting.ToJson();

            DA.SetData(0, json);
        }
    }
}