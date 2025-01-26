using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Converters;

public class GH_ObserverFromJson : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_ObserverFromJson class.
  /// </summary>
  public GH_ObserverFromJson()
    : base("Observer From Json", "OFJ",
      "Converts a JSON string to an observer",
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
    pManager.AddGenericParameter("Observer", "O", "The observer", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    // Declare a variable for the input
    var json = "";
    // If the input is not retrieved, return
    if (!DA.GetData(0, ref json)) return;

    if (json == "") return;

    var observer = EvolutionObserver.FromJson(json);

    DA.SetData(0, observer);
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
  public override Guid ComponentGuid => new("DC2277B8-3EBF-488E-86DA-8056B881A483");
}