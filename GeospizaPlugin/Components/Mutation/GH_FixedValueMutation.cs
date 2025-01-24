using System;
using System.Drawing;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Mutation;

public class GH_FixedValueMutation : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the FixedValueMutation class.
  /// </summary>
  public GH_FixedValueMutation()
    : base("Fixed Value Mutation", "Fixed Value Mutation",
      "Applies a fixed value mutation strategy in a genetic algorithm. This strategy alters genes of the " +
      "individuals in the population by a fixed value, aiding in the exploration " +
      "of the solution space. The mutation rate and value are adjustable parameters.",
      "Geospiza", "MutationStrategies")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddNumberParameter("Mutation Rate", "MR", "The mutation rate", GH_ParamAccess.item, 0.01);
    pManager.AddNumberParameter("Mutation Value", "MV",
      "Random int range of Mutation Value(+-MutationValue) to move the tick value", GH_ParamAccess.item, 5);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Mutation Strategy", "MS", "The mutation strategy", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    double mutationRate = 0;
    double mutationValue = 0;
    if (!DA.GetData(0, ref mutationRate)) return;
    if (!DA.GetData(1, ref mutationValue)) return;

    var strategy = new FixedValueMutation(mutationRate, Convert.ToInt32(mutationValue));

    DA.SetData(0, strategy);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.FixValueMutation;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("30348E4B-355D-4C85-8A6A-2ED2F7EB002D");
}