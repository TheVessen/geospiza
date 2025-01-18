using System;
using System.Drawing;
using GeospizaManager.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Webcomponents;

public class GH_PopulationToJSON : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_PopulationToJSON class.
  /// </summary>
  public GH_PopulationToJSON()
    : base("PopulationToJson", "PTJ",
      "Converts a population to a JSON string",
      "Geospiza", "Webcomponents")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Population", "P", "The population to convert to JSON", GH_ParamAccess.item);
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
    // Declare a variable for the input
    var populationWrapper = new GH_ObjectWrapper();
    // If the input is not retrieved, return
    if (!DA.GetData(0, ref populationWrapper)) return;

    if (populationWrapper.Value is Population population)
    {
      var json = population.ToJson();

      DA.SetData(0, json);
    }
  }

  public override GH_Exposure Exposure => GH_Exposure.hidden;

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
  public override Guid ComponentGuid => new("EED60EC5-E7F1-46B1-ACAD-7667A2565C35");
}