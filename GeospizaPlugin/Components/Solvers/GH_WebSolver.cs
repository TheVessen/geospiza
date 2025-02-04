using System;
using System.Collections.Generic;
using System.Threading;
using GeospizaCore.Core;
using GeospizaCore.Solvers;
using GeospizaPlugin.AsyncComponent;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino;

namespace GeospizaPlugin.Components.Solvers
{
    public class GH_WebSolver : GH_WebsocketComponent
    {
        public GH_WebSolver()
            : base("Web Solver", "WS",
                "A solver that can be triggered via WebSocket (only when 'Activate' is true).",
                "Geospiza", "Solvers")
        {
            BaseWorker = new WebSolverWorker(this);
        }

        public override Guid ComponentGuid => new Guid("eee606f7-f3ac-4739-bcb2-a511ac58412a");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Solver;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Genes", "GID", "The gene IDs from the GeneSelector", GH_ParamAccess.list);
            pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
                GH_ParamAccess.item);
            pManager.AddBooleanParameter("Activate", "A",
                "If TRUE, this component accepts 'run' commands over WebSocket.\nIf FALSE, remote triggers are ignored.",
                GH_ParamAccess.item, false);
            pManager.AddTextParameter("Endpoint", "E", "WebSocket endpoint", GH_ParamAccess.item,
                "ws://127.0.0.1:8181");
            pManager.AddGenericParameter("WebIndividual", "WI", "The individual to be displayed in the web interface",
                GH_ParamAccess.list);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Status or results", GH_ParamAccess.item);
            pManager.AddGenericParameter("Observer", "O", "The EvolutionObserver (last population info, etc.)",
                GH_ParamAccess.item);
        }
    }


    /// <summary>
    /// The synchronous worker for the WebSolver component.
    /// </summary>
    public class WebSolverWorker : WorkerInstance
    {
        private readonly object _lockObject = new object();
        private SolverSettings _settings;
        private StateManager _stateManager;
        private EvolutionObserver _evolutionObserver;
        private bool _activate;

        public WebSolverWorker(GH_Component parent)
            : base(parent)
        {
        }

        public override void GetData(IGH_DataAccess DA)
        {
            var geneIds = new List<string>();
            if (!DA.GetDataList(0, geneIds))
                return;

            SolverSettings settings = null;
            if (!DA.GetData(1, ref settings))
                return;

            bool activate = false;
            if (!DA.GetData(2, ref activate))
                return;

            string endpoint = "ws://127.0.0.1:8181";
            DA.GetData(3, ref endpoint);

            var stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
            var evolutionObserver = EvolutionObserver.GetInstance(Parent);

            stateManager.SetGenes(geneIds);
            stateManager.PreviewLevel = 3;

            lock (_lockObject)
            {
                _settings = settings;
                _stateManager = stateManager;
                _evolutionObserver = evolutionObserver;
                _activate = activate;
            }
        }

        public override void DoWork(Action Done, CancellationToken cancellationToken)
        {
            SolverSettings settings;
            StateManager stateManager;
            EvolutionObserver evolutionObserver;
            bool activate;

            lock (_lockObject)
            {
                settings = _settings;
                stateManager = _stateManager;
                evolutionObserver = _evolutionObserver;
                activate = _activate;
            }

            if (!activate)
            {
                Done();
                return;
            }

            stateManager.IsRunning = true;

            try
            {
                evolutionObserver.Reset();
                var solver = new BaseSolver(settings, stateManager, evolutionObserver);
                solver.RunAlgorithm(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Operation was canceled.
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Solver error: {ex.Message}");
            }
            finally
            {
                stateManager.IsRunning = false;
                Parent.OnPingDocument()?.ScheduleSolution(100, doc => doc.NewSolution(true));
                Done();
            }
        }

        public override void SetData(IGH_DataAccess DA)
        {
            //Set in the after solve instance
        }

        public override WorkerInstance Duplicate()
        {
            return new WebSolverWorker(Parent);
        }
    }
}