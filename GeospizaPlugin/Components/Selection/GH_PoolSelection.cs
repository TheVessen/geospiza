using System;
using System.Drawing;
using GeospizaManager.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Selection;

public class GH_PoolSelection : GH_Component
{
  /// <summary>
  ///   Initializes a new instance of the GH_PoolSelection class.
  /// </summary>
  public GH_PoolSelection()
    : base("PoolSelection", "PS",
      "Performs a pool selection. In Pool Selection, each individual in the population is assigned a selection probability " +
      "proportional to its fitness. Then, a number of individuals are selected randomly based on these probabilities.",
      "Geospiza", "SelectionStrategy")
  {
  }

  /// <summary>
  ///   Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Resources.PoolSelection;

  /// <summary>
  ///   Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("3C6ACC4C-5C95-4189-BC61-19A4D870E554");

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

  /// <summary>
  ///   This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var selection = new PoolSelection();

    DA.SetData(0, selection);
  }
}