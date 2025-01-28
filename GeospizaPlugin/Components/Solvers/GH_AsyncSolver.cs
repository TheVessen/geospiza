using GeospizaCore.Core;
using GeospizaCore.Solvers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using GeospizaPlugin.AsyncComponent;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Solvers
{
    public class GH_AsyncSolver : GH_AsyncComponent
    {
        public GH_AsyncSolver()
            : base("Async Solver", "AS",
                "Solver for single objective optimization problems",
                "Geospiza", "Solvers")
        {
            BaseWorker = new EvoAsyncWorker(this);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.Solver;
        public override Guid ComponentGuid => new Guid("DC3BBA6C-499E-496C-AE42-5488B065C38F");

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Genes", "GID", "The gene ids from the GeneSelector", GH_ParamAccess.list);
            pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
                GH_ParamAccess.item);

            var updateParam = new Param_Integer();
            updateParam.AddNamedValue("All", 0);
            updateParam.AddNamedValue("Every Generation", 1);
            updateParam.AddNamedValue("If Better", 2);
            updateParam.AddNamedValue("None", 3);
            updateParam.PersistentData.Append(new GH_Integer(0));
            pManager.AddParameter(updateParam, "Preview Level", "PL", "Set how often the preview should update.",
                GH_ParamAccess.item);

            pManager.AddIntegerParameter("Timestamp", "T",
                "Timestamp from the server to determine if the solver should run",
                GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Run", "R", "Run the solver for running locally", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Observer", "LP", "The last population", GH_ParamAccess.item);
            pManager.AddGenericParameter("State Manager", "SM", "The state managers", GH_ParamAccess.item);
            pManager.AddNumberParameter("Current Generation", "CG", "The current generation", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Is Running", "IR", "Is the solver running", GH_ParamAccess.item);
        }

        private class EvoAsyncWorker : WorkerInstance
        {
            private readonly object _lockObject = new object();
            private List<string> _geneIds;
            private SolverSettings _privateSettings;
            private int _previewLevel;
            private long _timestamp;
            private bool _run;
            private EvolutionObserver _evolutionObserver;
            private StateManager _stateManager;
            private bool _isRunning = false;
            private long _lastTimestamp = 0;
            private Guid _solutionId = Guid.Empty;
            private Guid _lastSolutionId = Guid.Empty;

            public EvoAsyncWorker(GH_Component parent) : base(parent) { }

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                // Temporary local variables; only commit them under lock at the end.
                var geneIds = new List<string>();
                if (!DA.GetDataList(0, geneIds))
                    return;  // If no input, do nothing.

                var settings = new SolverSettings();
                if (!DA.GetData(1, ref settings))
                    return;

                var previewLevel = 0;
                if (!DA.GetData(2, ref previewLevel))
                    return;

                int timestampInt = 0;
                if (!DA.GetData(3, ref timestampInt))
                    return;
                var intTimestamp = Convert.ToInt64(timestampInt);

                bool run = false;
                if (!DA.GetData(4, ref run))
                    return;
                
                var stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
                var evolutionObserver = EvolutionObserver.GetInstance(Parent);

                // Commit everything in a lock so the fields update atomically.
                lock (_lockObject)
                {
                    _geneIds = geneIds;
                    _privateSettings = settings;
                    _previewLevel = previewLevel;
                    _timestamp = intTimestamp;
                    _run = run;

                    _stateManager = stateManager;
                    _evolutionObserver = evolutionObserver;
                }
            }

            /// <summary>
            /// This method is invoked on the Grasshopper main thread after
            /// we schedule a solution (via doc.ScheduleSolution).
            /// Here we actually run the solver (blocking in this example).
            /// </summary>
            private void ScheduleCallback(GH_Document doc)
            {
                Guid currentSolutionId;

                // Lock to guard against multiple parallel solver runs.
                lock (_lockObject)
                {
                    // If something changed or we were cancelled, we could bail out here.
                    if (_isRunning) return;

                    // Mark as running, assign a new solution ID
                    _isRunning = true;
                    currentSolutionId = Guid.NewGuid();
                    _solutionId = currentSolutionId;
                }

                try
                {
                    EvolutionObserver observer;
                    SolverSettings settings;
                    StateManager sm;

                    // Copy references inside the lock for safe usage outside it
                    lock (_lockObject)
                    {
                        observer = _evolutionObserver;
                        settings = _privateSettings;
                        sm = _stateManager;
                    }

                    // Reset the observer if needed
                    observer.Reset();

                    // We do a synchronous call to run the solver here
                    var evolutionaryAlgorithm = new BaseSolver(settings, sm, observer);
                    evolutionaryAlgorithm.RunAlgorithm();
                }
                finally
                {
                    lock (_lockObject)
                    {
                        if (_solutionId == currentSolutionId)
                        {
                            _lastSolutionId = currentSolutionId;
                            _isRunning = false;
                        }
                    }

                    doc.ScheduleSolution(100, doc2 =>
                    {
                        doc2.NewSolution(true);
                    });
                }
            }

            public override WorkerInstance Duplicate() => new EvoAsyncWorker(Parent);

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                // If cancellation is requested, just bail out.
                if (CancellationToken.IsCancellationRequested)
                {
                    Done();
                    return;
                }

                bool shouldStart;
                long localTimestamp;
                bool localRun;
                List<string> localGeneIds;

                // Read the necessary fields under lock
                lock (_lockObject)
                {
                    localRun = _run;
                    localTimestamp = _timestamp;
                    localGeneIds = _geneIds;
                    
                    // Decide if we should start a new solver run
                    // Conditions:
                    // 1) The "Run" input is true, or
                    // 2) There's a new timestamp from the server
                    // 3) AND we are not already running
                    shouldStart = (localRun || (localTimestamp != 0 && localTimestamp != _lastTimestamp)) && !_isRunning;

                    if (shouldStart)
                    {
                        // Update the state manager with new gene IDs
                        _stateManager.SetGenes(localGeneIds);
                        _stateManager.PreviewLevel = _previewLevel;

                        // Update lastTimestamp so we don't trigger again
                        _lastTimestamp = localTimestamp;
                    }
                }

                if (shouldStart)
                {
                    // Report that we are running (for progress display in GH)
                    // Example: "Running" plus the current generation index
                    lock (_lockObject)
                    {
                        // Safely read generation index from observer
                        if (_evolutionObserver != null)
                            ReportProgress("Running", _evolutionObserver.CurrentGenerationIndex);
                        else
                            ReportProgress("Running", 0);
                    }

                    // Schedule a new solution after 100 ms, which calls our solver
                    // on the Grasshopper main thread:
                    Parent.OnPingDocument().ScheduleSolution(100, ScheduleCallback);
                }

                // We must always call Done() or Grasshopper thinks the worker is still busy.
                Done();
            }

            public override void SetData(IGH_DataAccess DA)
            {
                // We'll read these shared fields atomically
                bool currentIsRunning;
                EvolutionObserver currentObserver;
                StateManager currentStateManager;
                double currentGenIndex;

                lock (_lockObject)
                {
                    currentIsRunning = _isRunning;
                    currentObserver = _evolutionObserver;
                    currentStateManager = _stateManager;
                    currentGenIndex = currentObserver?.CurrentGenerationIndex ?? 0;
                }

                // Send data out
                DA.SetData(0, currentObserver);
                DA.SetData(1, currentStateManager);
                DA.SetData(2, currentGenIndex);
                DA.SetData(3, currentIsRunning);
            }
        }
    }
}
