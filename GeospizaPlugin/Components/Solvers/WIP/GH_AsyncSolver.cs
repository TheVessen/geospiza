// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Threading;
// using GeospizaManager.Core;
// using GeospizaManager.Solvers;
// using Grasshopper.Kernel;
// using Grasshopper.Kernel.Parameters;
// using Grasshopper.Kernel.Types;
//
// namespace GeospizaPlugin.Components.Solvers;
//
// public class GH_AsyncSolver : AsyncComponent
// {
//   /// <summary>
//   /// Initializes a new instance of the BasicSolver class.
//   /// </summary>
//   public GH_AsyncSolver()
//     : base("GH_AsyncSolver", "SOS",
//       "Solver for single objective optimization problems",
//       "Geospiza", "Solvers")
//   {
//     BaseWorker = new EvoAsyncWorker(this);
//   }
//
//   /// <summary>
//   /// Registers all the input parameters for this component.
//   /// </summary>
//   protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//   {
//     pManager.AddTextParameter("Genes", "GID", "The gene ids from the GeneSelector", GH_ParamAccess.list);
//     pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm", GH_ParamAccess.item);
//
//     // Preview level
//     Param_Integer updateParam = new Param_Integer();
//     updateParam.AddNamedValue("All", 0);
//     updateParam.AddNamedValue("EveryGeneration", 1);
//     updateParam.AddNamedValue("IfBetter", 2);
//     updateParam.AddNamedValue("None", 3);
//     updateParam.PersistentData.Append(new GH_Integer(0));
//     pManager.AddParameter(updateParam, "PreviewLevel", "PL", "Set how often the preview should update.",
//       GH_ParamAccess.item);
//
//     pManager.AddNumberParameter("Timestamp", "T", "Timestamp from the server to determine if the solver should run",
//       GH_ParamAccess.item, 0);
//     pManager.AddBooleanParameter("Run", "R", "Run the solver for running locally", GH_ParamAccess.item, false);
//   }
//
//   /// <summary>
//   /// Registers all the output parameters for this component.
//   /// </summary>
//   protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//   {
//     pManager.AddGenericParameter("Observer", "LP", "The last population", GH_ParamAccess.item);
//     pManager.AddGenericParameter("StateManager", "SM", "The state managers", GH_ParamAccess.item);
//     pManager.AddNumberParameter("CurrentGeneration", "CG", "The current generation", GH_ParamAccess.item);
//     pManager.AddBooleanParameter("IsRunning", "IR", "Is the solver running", GH_ParamAccess.item);
//   }
//
//   /// <summary>
//   /// This is the method that actually does the work.
//   /// </summary>
//   /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//   protected override void SolveInstance(IGH_DataAccess DA)
//   {
//   }
//
//   /// <summary>
//   /// Provides an Icon for the component.
//   /// </summary>
//   protected override Bitmap Icon => Properties.Resources.Solver;
//
//   /// <summary>
//   /// Gets the unique ID for this component. Do not change this ID after release.
//   /// </summary>
//   public override Guid ComponentGuid
//   {
//     get { return new Guid("DC3BBA6C-488E-496C-AE42-5488B065C38F"); }
//   }
//
//   private class EvoAsyncWorker : WorkerInstance
//   {
//     public EvoAsyncWorker(GH_Component _parent) : base(_parent)
//     {
//       var doc = _parent.OnPingDocument();
//       _stateManager = StateManager.GetInstance(_parent,doc);
//       _evolutionObserver = EvolutionObserver.GetInstance(_parent);
//       this._parent = _parent;
//     }
//
//     public double Num { get; set; }
//     private List<string> GeneIds { get; set; }
//     private SolverSettings _privateSettings;
//     private int PreviewLevel { get; set; }
//     private long Timestamp { get; set; }
//     private bool Run { get; set; }
//     private readonly StateManager _stateManager;
//     private readonly EvolutionObserver _evolutionObserver;
//     private long _lastTimestamp = 0;
//     private bool _isRunning = false;
//     private Guid _solutionId = Guid.NewGuid();
//     private Guid _lastSolutionId;
//     private TimeSpan _time;
//     private GH_Component _parent;
//
//     public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
//     {
//       // Get inputs
//       var geneIds = new List<string>();
//       if (!DA.GetDataList(0, geneIds)) return;
//
//       var settings = new SolverSettings();
//       if (!DA.GetData(1, ref settings)) return;
//       _privateSettings = settings;
//
//       var previewLevel = 0;
//       if (!DA.GetData(2, ref previewLevel)) return;
//
//       long timestamp = 0;
//       if (!DA.GetData(3, ref timestamp)) return;
//       long intTimestamp = Convert.ToInt64(timestamp);
//
//       var run = false;
//       if (!DA.GetData(4, ref run)) return;
//
//
//       // Set data
//       GeneIds = geneIds;
//       PreviewLevel = previewLevel;
//       Timestamp = timestamp;
//       Run = run;
//     }
//
//     /// <summary>
//     /// Scedules the callback for the solver
//     /// </summary>
//     /// <param name="doc"></param>
//     private void ScheduleCallback(GH_Document doc)
//     {
//       var start = DateTime.Now;
//       _solutionId = Guid.NewGuid();
//       _evolutionObserver.Reset();
//
//       var evolutionaryAlgorithm = new BaseSolver(_privateSettings, _stateManager, _evolutionObserver);
//
//       evolutionaryAlgorithm.RunAlgorithm();
//
//       _parent.OnPingDocument().ScheduleSolution(100, doc =>
//       {
//         _isRunning = false;
//         _parent.ExpireSolution(false);
//       });
//
//       _lastSolutionId = _solutionId;
//       var end = DateTime.Now;
//       var time = end - start;
//     }
//
//     // Synchronization event
//     private ManualResetEvent dataCollectedEvent = new ManualResetEvent(false);
//
//
//     public override WorkerInstance Duplicate() => new EvoAsyncWorker(_parent);
//
//     public override void DoWork(Action<string, double> ReportProgress, Action Done)
//     {
//       if (CancellationToken.IsCancellationRequested) return;
//
//       _evolutionObserver.Reset();
//       // Check if the solver was triggered by the button or timestamp
//       if (Run)
//       {
//         // Solver was triggered by the button
//       }
//       else if (Timestamp != 0 && Timestamp != _lastTimestamp)
//       {
//       }
//
//       // Check if the solver should run
//       if (_lastSolutionId != Guid.Empty && _solutionId != _lastSolutionId)
//       {
//         return;
//       }
//       
//       _stateManager.SetGenes(GeneIds);
//       _stateManager.PreviewLevel = PreviewLevel;
//
//       // Check if the solver should run
//       bool start = (Timestamp != 0 && Timestamp != _lastTimestamp) || Run;
//
//       // Run the solver
//       if (start)
//       {
//         ReportProgress("Running", _evolutionObserver.CurrentGenerationIndex);
//         _isRunning = true;
//         _parent.OnPingDocument().ScheduleSolution(100, ScheduleCallback);
//         _lastTimestamp = Timestamp;
//       }
//
//       Done();
//     }
//
//     public override void SetData(IGH_DataAccess DA)
//     {
//       if (_isRunning == false)
//       {
//         DA.SetData(0, _evolutionObserver);
//         DA.SetData(1, _stateManager);
//       }
//
//       DA.SetData(2, _evolutionObserver.CurrentGenerationIndex);
//       DA.SetData(3, _isRunning);
//     }
//   }
// }

