using System;
using System.Drawing;
using System.Linq;
using GeospizaCore.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Utils;

public class GH_ReinstateIndividual : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the ReinstateIndividual class.
  /// </summary>
  public GH_ReinstateIndividual()
    : base("Reinstate Individual", "Reinstate Individual",
      "Reactivates a previously archived individual in the current population",
      "Geospiza", "Utils")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("State Manager", "SM", "The state managers", GH_ParamAccess.item);
    pManager.AddGenericParameter("Individual", "I", "The individual to reinstate", GH_ParamAccess.tree);
    pManager.AddBooleanParameter("Reinstate", "R", "Reinstate the individual", GH_ParamAccess.item, false);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
  }

  private Individual individual;
  private StateManager stateManager;

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    GH_ObjectWrapper stateManagerWrapper = null;
    if (!DA.GetData(0, ref stateManagerWrapper)) return;
    stateManager = stateManagerWrapper.Value as StateManager;

    var individualWrapper = new GH_Structure<IGH_Goo>();
    if (!DA.GetDataTree(1, out individualWrapper)) return;

    var data = individualWrapper.AllData(true).ToList();
    if (data.Count == 0) return;

    if (data.Count != 1)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a single individual");
      return;
    }

    individual = data[0].ScriptVariable() as Individual;
    var reinstate = false;
    if (!DA.GetData(2, ref reinstate)) return;

    if (reinstate) OnPingDocument().ScheduleSolution(10, ScheduleCallback);
  }

  private void ScheduleCallback(GH_Document doc)
  {
    OnPingDocument().NewSolution(false);
    individual.Reinstate(stateManager);
    ExpirePreview(false);
    ExpireSolution(false);
  }

  public override GH_Exposure Exposure => GH_Exposure.tertiary;

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.ReinstateIndividuum;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("6692F05D-9C4F-4B99-B4C5-CF1D91C7B639");
}