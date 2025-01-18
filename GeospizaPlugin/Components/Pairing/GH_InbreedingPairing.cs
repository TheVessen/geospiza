using System;
using System.Drawing;
using GeospizaManager.Strategies;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Pairing;

public class GH_InbreedingPairing : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the InbreedingPairing class.
  /// </summary>
  public GH_InbreedingPairing()
    : base("InbreedingPairing", "IP",
      "This code is a part of a genetic algorithm that pairs individuals based on their genetic similarity or dissimilarity. " +
      "It uses an in-breeding factor to determine the preference for selecting mates.",
      "Geospiza", "PairingStrategies")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddNumberParameter("InBreedingFactor", "P",
      "Inbreeding Factor, a value between 0 and 1. A value of 0 promotes pairing between individuals with high genetic similarity, " +
      "while a value of 1 encourages pairing between individuals with high genetic dissimilarity.", GH_ParamAccess.item,
      0.2);
    pManager.AddNumberParameter("DistanceFunction", "DF",
      "The distance function to use. 0 for euclidean, 1 for manhattan", GH_ParamAccess.item, 1);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("PairingStrategy", "PS", "The pairing strategy", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    double inBreedingFactor = 0;
    if (!DA.GetData(0, ref inBreedingFactor)) return;
    double distanceFunction = 0;
    if (!DA.GetData(1, ref distanceFunction)) return;
    var distanceFunctionInt = Convert.ToInt32(distanceFunction);
    if (distanceFunctionInt is > 1 or < 0)
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Distance function must be 0 or 1");

    DistanceFunctionType df;
    if (distanceFunction == 0)
      df = DistanceFunctionType.Euclidean;
    else
      df = DistanceFunctionType.Manhattan;

    var pairing = new PairingStrategy(inBreedingFactor, df);

    DA.SetData(0, pairing);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.InBreedingStrategy;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("86B5A4E3-080A-4419-A0AD-FD42CB4890F5");
}