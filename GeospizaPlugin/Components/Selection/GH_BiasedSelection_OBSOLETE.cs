using System;
using System.Drawing;
using GeospizaManager.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Selection;

public class GH_BiasedSelection_OBSOLETE : GH_Component
{
  /// <summary>
  ///   Initializes a new instance of the GH_BiasedSelection_OBSOLETE class.
  /// </summary>
  public GH_BiasedSelection_OBSOLETE()
    : base("BiasedSelection", "BS",
      "Performs a biased selection. In Biased Selection, each individual in the population is assigned " +
      "a selection probability proportional to its fitness.Then, " +
      "a number of individuals are selected randomly based on these probabilities.",
      "Geospiza", "SelectionStrategy")
  {
  }

  /// <summary>
  ///   Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Resources.BiasSelection;

  /// <summary>
  ///   Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("2A14D98B-C387-4C60-8EAC-28D821DD7948");

  /// <summary>
  ///   Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
  }

  /// <summary>
  ///   Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("SelectionStrategy", "SS", "The selection strategy", GH_ParamAccess.item);
  }

  public override GH_Exposure Exposure { get; } = GH_Exposure.hidden;

  /// <summary>
  ///   This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var selection = new RouletteWheelSelection();

    DA.SetData(0, selection);
  }
}