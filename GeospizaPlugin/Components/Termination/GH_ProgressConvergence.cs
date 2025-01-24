using System;
using System.Drawing;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Termination;

public class GH_ProgressConvergence : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the ProgressConvergence class.
  /// </summary>
  public GH_ProgressConvergence()
    : base("Progress Convergence", "PC",
      "Terminates the genetic algorithm when the progress over the last 5 generations is below a certain threshold",
      "Geospiza", "Termination Strategies")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddNumberParameter("Threshold", "T", "The threshold for the genetic diversity", GH_ParamAccess.item, 0.1);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Termination Strategy", "TS", "The termination strategy", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    double threshold = 0;
    if (!DA.GetData(0, ref threshold)) return;
    DA.SetData(0, new ProgressConvergence(threshold));
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.TerminationGenetic;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("362B7670-54AA-4B7D-80D2-FAF6A4B2A0D9");
}