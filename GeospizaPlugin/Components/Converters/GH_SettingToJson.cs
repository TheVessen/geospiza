using System;
using System.Drawing;
using GeospizaManager.Solvers;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Converters;

public class GH_SettingToJson : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the SettingToJson class.
  /// </summary>
  public GH_SettingToJson()
    : base("Setting To Json", "Setting To Json",
      "Converts a setting to a JSON string",
      "Geospiza", "Converter")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Setting", "S", "The setting to convert to JSON", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
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

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => null;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("B6D9077C-391E-4DB7-B954-0CE7A2C1A333");
}