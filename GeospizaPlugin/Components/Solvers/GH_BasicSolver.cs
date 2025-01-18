using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaManager.Core;
using GeospizaManager.Solvers;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Solvers;

public class GH_BasicSolver : GH_Component
{
  private StateManager StateManager { get; set; }
  private EvolutionObserver EvolutionObserver { get; set; }
  private long _lastTimestamp;
  private SolverSettings _privateSettings;
  private bool _isRunning;
  private Guid _solutionId = Guid.NewGuid();
  private Guid _lastSolutionId;

  /// <summary>
  /// Initializes a new instance of the BasicSolver class.
  /// </summary>
  public GH_BasicSolver()
    : base("SingleObjectiveSolver", "SOS",
      "Solver for single objective optimization problems",
      "Geospiza", "Solvers")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    // Gene IDs
    pManager.AddTextParameter(
      "Genes",
      "GID",
      "The gene IDs from the GeneSelector",
      GH_ParamAccess.list
    );

    // Solver settings
    pManager.AddGenericParameter(
      "Settings",
      "S",
      "The settings for the evolutionary algorithm",
      GH_ParamAccess.item
    );

    // Preview Level
    var updateParam = new Param_Integer();
    updateParam.AddNamedValue("All", 0);
    updateParam.AddNamedValue("EveryGeneration", 1);
    updateParam.AddNamedValue("IfBetter", 2);
    updateParam.AddNamedValue("None", 3);
    updateParam.PersistentData.Append(new GH_Integer(0));
    pManager.AddParameter(
      updateParam,
      "PreviewLevel",
      "PL",
      "How often the preview should update:\n" +
      "• 0: Every solution\n" +
      "• 1: Every generation\n" +
      "• 2: Only if a better solution is found\n" +
      "• 3: None (no preview)\n\n" +
      "More frequent preview updates take longer.",
      GH_ParamAccess.item
    );

    // Timestamp (for network-based triggers, etc.)
    pManager.AddNumberParameter(
      "Timestamp",
      "T",
      "Timestamp from the server to determine if the solver should run",
      GH_ParamAccess.item,
      0
    );

    // Run (for local/manual triggers)
    pManager.AddBooleanParameter(
      "Run",
      "R",
      "Set to true to run the solver locally",
      GH_ParamAccess.item,
      false
    );
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Observer", "LP", "The EvolutionObserver (last population info, etc.)",
      GH_ParamAccess.item);
    pManager.AddGenericParameter("StateManager", "SM", "The StateManager handling gene states", GH_ParamAccess.item);
    pManager.AddNumberParameter("CurrentGeneration", "CG", "The current generation index", GH_ParamAccess.item);
    pManager.AddBooleanParameter("IsRunning", "IR", "Indicates whether the solver is running", GH_ParamAccess.item);
  }

  /// <summary>
  /// This method actually does the work.
  /// </summary>
  /// <param name="DA">Data access interface for inputs/outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    // Lazy-initialize singletons
    StateManager ??= StateManager.GetInstance(this, OnPingDocument());
    EvolutionObserver ??= EvolutionObserver.GetInstance(this);

    // Get inputs
    var geneIds = new List<string>();
    if (!DA.GetDataList(0, geneIds)) return;

    var settings = new SolverSettings();
    if (!DA.GetData(1, ref settings)) return;
    _privateSettings = settings;

    var previewLevel = 0;
    if (!DA.GetData(2, ref previewLevel)) return;

    double timestampDouble = 0;
    if (!DA.GetData(3, ref timestampDouble)) return;
    var currentTimestamp = Convert.ToInt64(timestampDouble);

    var runButton = false;
    if (!DA.GetData(4, ref runButton)) return;

    // Decide whether a new run is triggered
    var buttonTriggered = runButton;
    var timestampTriggered = currentTimestamp != 0 && currentTimestamp != _lastTimestamp;

    if (_lastSolutionId != Guid.Empty && _solutionId != _lastSolutionId)
      // We have a new solution ID that doesn't match the last used solution ID
      // If you intend to allow multiple subsequent runs, adjust or remove this check
      return;

    // Update state manager
    StateManager.SetGenes(geneIds);
    StateManager.PreviewLevel = previewLevel;

    var shouldStartSolver = buttonTriggered || timestampTriggered;

    // Run the solver if triggered
    if (shouldStartSolver)
    {
      EvolutionObserver.Reset();
      DA.SetData(0, null); // Clear the observer output before we run
      _isRunning = true;

      // Schedule the solver work in 100 ms
      OnPingDocument().ScheduleSolution(100, ScheduleCallback);
      _lastTimestamp = currentTimestamp;
    }
  }

  /// <summary>
  /// Schedules the actual solver execution.
  /// </summary>
  /// <param name="doc">Grasshopper document reference.</param>
  private void ScheduleCallback(GH_Document doc)
  {
    var start = DateTime.Now;
    _solutionId = Guid.NewGuid();

    // Construct and run the solver
    var evolutionaryAlgorithm = new BaseSolver(_privateSettings, StateManager, EvolutionObserver);
    evolutionaryAlgorithm.RunAlgorithm();

    // Once complete, schedule a new solution to update outputs
    OnPingDocument().ScheduleSolution(100, d =>
    {
      _isRunning = false;
      ExpireSolution(false);
    });

    _lastSolutionId = _solutionId;
    var end = DateTime.Now;
    var elapsed = end - start;
    // If needed, you could store or display 'elapsed'
  }

  /// <summary>
  /// Cleanup the component after SolveInstance has run.
  /// </summary>
  protected override void AfterSolveInstance()
  {
    base.AfterSolveInstance();

    // Write outputs after the solver finishes
    if (!_isRunning)
    {
      // Observer
      Params.Output[0].ClearData();
      Params.Output[0].AddVolatileData(new GH_Path(0), 0, EvolutionObserver);

      // State manager
      Params.Output[1].ClearData();
      Params.Output[1].AddVolatileData(new GH_Path(0), 0, StateManager);
    }

    // Current generation
    Params.Output[2].ClearData();
    Params.Output[2].AddVolatileData(new GH_Path(0), 0, EvolutionObserver.CurrentGenerationIndex);

    // IsRunning
    Params.Output[3].ClearData();
    Params.Output[3].AddVolatileData(new GH_Path(0), 0, _isRunning);
  }

  /// <summary>
  /// Provides an icon for the component.
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.Solver;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("DC3BBA6C-488E-496C-AE62-5488B065C38F");
}