using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Converters;

public class GH_IndividualFromJson : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_JsonToIndividual class.
  /// </summary>
  public GH_IndividualFromJson()
    : base("Individual From Json", "Individual From Json",
      "Converts a JSON string to an individual",
      "Geospiza", "Converter")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Individual", "I", "The individual", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var json = "";
    if (!DA.GetData(0, ref json)) return;

    if (json == "") return;

    var individual = Individual.FromJson(json);

    DA.SetData(0, individual);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon =>
    //You can add image files to your project resources and access them like this:
    // return Resources.IconForThisComponent;
    null;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("7DAE3EE7-F6F1-4F73-BA81-753B2ABBB617");
}