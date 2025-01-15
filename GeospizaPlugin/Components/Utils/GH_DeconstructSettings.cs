using System;
using System.Drawing;
using GeospizaManager.Solvers;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Utils;

public class GH_DeconstructSettings : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_DeconstructSettings class.
  /// </summary>
  public GH_DeconstructSettings()
    : base("DeconstructSettings", "DeconstructSettings",
      "Deconstructs the settings for the evolutionary algorithm",
      "Geospiza", "Utils")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("SelectionStrategy", "SS", "The selection strategy", GH_ParamAccess.item);
    pManager.AddGenericParameter("CrossoverStrategy", "CS", "The crossover strategy", GH_ParamAccess.item);
    pManager.AddGenericParameter("MutationStrategy", "MS", "The mutation strategy", GH_ParamAccess.item);
    pManager.AddGenericParameter("PairingStrategy", "PS", "The pairing strategy", GH_ParamAccess.item);
    pManager.AddGenericParameter("TerminationStrategy", "TS", "The termination strategy", GH_ParamAccess.item);
    pManager.AddIntegerParameter("PopulationSize", "PS", "The population size", GH_ParamAccess.item);
    pManager.AddIntegerParameter("MaxGenerations", "MG", "The maximum number of generations", GH_ParamAccess.item);
    pManager.AddIntegerParameter("EliteSize", "ES", "The elite size", GH_ParamAccess.item);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    // Variables
    var settings = new EvolutionaryAlgorithmSettings();

    // Set variables
    DA.GetData(0, ref settings);

    // Output
    DA.SetData(0, settings.SelectionStrategy);
    DA.SetData(1, settings.CrossoverStrategy);
    DA.SetData(2, settings.MutationStrategy);
    DA.SetData(3, settings.PairingStrategy);
    DA.SetData(4, settings.TerminationStrategy);
    DA.SetData(5, settings.PopulationSize);
    DA.SetData(6, settings.MaxGenerations);
    DA.SetData(7, settings.EliteSize);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon
  {
    get
    {
      //You can add image files to your project resources and access them like this:
      // return Resources.IconForThisComponent;
      return null;
    }
  }

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid
  {
    get { return new Guid("12F671BE-8BF8-4B91-B532-D94B80FA3F71"); }
  }
}