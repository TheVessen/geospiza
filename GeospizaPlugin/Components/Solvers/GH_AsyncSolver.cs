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
            BaseWorker = new EvoAsyncWorker();
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.Solver;
        public override Guid ComponentGuid => new Guid("DC3BBA6C-499E-496C-AE42-5488B065C38F");
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Genes", "GID", "The gene ids from the GeneSelector", GH_ParamAccess.list);
            pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
                GH_ParamAccess.item);
            
            Param_Integer updateParam = new Param_Integer();
            updateParam.AddNamedValue("All", 0);
            updateParam.AddNamedValue("Every Generation", 1);
            updateParam.AddNamedValue("If Better", 2);
            updateParam.AddNamedValue("None", 3);
            updateParam.PersistentData.Append(new GH_Integer(0));
            pManager.AddParameter(updateParam, "Preview Level", "PL", "Set how often the preview should update.",
                GH_ParamAccess.item);

            pManager.AddNumberParameter("Timestamp", "T",
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
            public double Num { get; set; }
            private List<string> GeneIds { get; set; }
            private SolverSettings _privateSettings;
            private int PreviewLevel { get; set; }
            private long Timestamp { get; set; }
            private bool Run { get; set; }
            private  EvolutionObserver _evolutionObserver;
            private long _lastTimestamp = 0;
            private bool _isRunning = false;
            private Guid _solutionId = Guid.NewGuid();
            private Guid _lastSolutionId;
            private TimeSpan _time;
            private StateManager _stateManager;
            
            public EvoAsyncWorker() : base(null)
            {

            }

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                var geneIds = new List<string>();
                if (!DA.GetDataList(0, geneIds)) return;

                var settings = new SolverSettings();
                if (!DA.GetData(1, ref settings)) return;
                _privateSettings = settings;

                var previewLevel = 0;
                if (!DA.GetData(2, ref previewLevel)) return;

                double timestamp = 0;
                if (!DA.GetData(3, ref timestamp)) return;
                long intTimestamp = Convert.ToInt64(timestamp);

                var run = false;
                if (!DA.GetData(4, ref run)) return;
                
                if(_stateManager == null)
                    _stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
                
                if(_evolutionObserver == null)
                    _evolutionObserver = EvolutionObserver.GetInstance(Parent);

                GeneIds = geneIds;
                PreviewLevel = previewLevel;
                Timestamp = intTimestamp;
                Run = run;
            }

            private void ScheduleCallback(GH_Document doc)
            {
                var start = DateTime.Now;
                _solutionId = Guid.NewGuid();
                _evolutionObserver.Reset();
                
                var evolutionaryAlgorithm = new BaseSolver(_privateSettings, _stateManager, _evolutionObserver);
                
                evolutionaryAlgorithm.RunAlgorithm();
                
                Parent.OnPingDocument().ScheduleSolution(100, doc =>
                {
                    _isRunning = false;
                    Parent.ExpireSolution(false);
                });
                
                _lastSolutionId = _solutionId;
                var end = DateTime.Now;
                var time = end - start;
            }

            public override WorkerInstance Duplicate() => new EvoAsyncWorker();

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (CancellationToken.IsCancellationRequested) return;

                _evolutionObserver.Reset();
                if (Run)
                {
                }
                else if (Timestamp != 0 && Timestamp != _lastTimestamp)
                {
                }
                
                if (_lastSolutionId != Guid.Empty && _solutionId != _lastSolutionId)
                {
                    return;
                }
                
                _stateManager.SetGenes(GeneIds);
                _stateManager.PreviewLevel = PreviewLevel;
                
                var start = (Timestamp != 0 && Timestamp != _lastTimestamp) || Run;
                
                if (start)
                {
                    ReportProgress("Running", _evolutionObserver.CurrentGenerationIndex);
                    _isRunning = true;
                    Parent.OnPingDocument().ScheduleSolution(100, ScheduleCallback);
                    _lastTimestamp = Timestamp;
                }

                Done();
            }

            public override void SetData(IGH_DataAccess DA)
            {
                if (_isRunning == false)
                {
                    DA.SetData(0, _evolutionObserver);
                    DA.SetData(1, _stateManager);
                }

                DA.SetData(2, _evolutionObserver.CurrentGenerationIndex);
                DA.SetData(3, _isRunning);
            }
        }
    }
}