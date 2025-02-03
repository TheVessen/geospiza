using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaCore.Solvers;
using GeospizaPlugin.AsyncComponent;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

//Attention this component combined with the WebSolverWorker work, but its quite a mess and needs to get a proper cleanup

namespace GeospizaPlugin.Components.Solvers;

public class GH_WebSolver : GH_WebsocketComponent
{
    public GH_WebSolver()
        : base("Web Solver", "WS",
            "A solver that can be triggered via WebSocket (only when 'Activate' is true).",
            "Geospiza", "Solvers")
    {
        BaseWorker = new WebSolverWorker(this);
    }

    public override Guid ComponentGuid => new("eee606f7-f3ac-4739-bcb2-a511ac58412a");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Resources.Solver;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter(
            "Genes",
            "GID",
            "The gene IDs from the GeneSelector",
            GH_ParamAccess.list
        );

        pManager.AddGenericParameter(
            "Settings",
            "S",
            "The settings for the evolutionary algorithm",
            GH_ParamAccess.item
        );


        pManager.AddBooleanParameter(
            "Activate",
            "A",
            "If TRUE, this component accepts 'run' commands over WebSocket.\nIf FALSE, remote triggers are ignored.",
            GH_ParamAccess.item,
            false
        );

        pManager.AddTextParameter(
            "Endpoint",
            "E",
            "WebSocket endpoint (optional). Currently not used to auto-restart the server, but can be used for display.",
            GH_ParamAccess.item,
            "ws://127.0.0.1:8181"
        );

        pManager.AddGenericParameter("WebIndividual", "WI", "The individual to be displayed in the web interface",
            GH_ParamAccess.list);
        pManager[4].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter(
            "Message",
            "M",
            "Status or results",
            GH_ParamAccess.item
        );

        pManager.AddGenericParameter("Observer", "O", "The EvolutionObserver (last population info, etc.)",
            GH_ParamAccess.item);
    }

    public class WebSolverWorker : WorkerInstance
    {
        private readonly object _lockObject = new();
        private EvolutionBlueprint _blueprint;
        public EvolutionObserver _evolutionObserver;
        private bool _isRunning;
        private SolverSettings _settings;
        public StateManager _stateManager;

        public WebSolverWorker(GH_Component parent) : base(parent)
        {
        }

        public override void DoWork(Action<string, double> ReportProgress, Action Done)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Done();
                return;
            }

            try
            {
                ReportProgress("Running", 0);
                _evolutionObserver.Reset();
                _stateManager.IsRunning = true;
                _blueprint.RunAlgorithm(CancellationToken);
            }
            finally
            {
                _isRunning = false;
                _stateManager.IsRunning = false;
                Parent.OnPingDocument()?.ScheduleSolution(100, doc => doc.NewSolution(true));
                Done();
            }
        }

        public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
        {
            var geneIds = new List<string>();
            if (!DA.GetDataList(0, geneIds)) return;

            var settings = new SolverSettings();
            if (!DA.GetData(1, ref settings)) return;

            var stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
            var evolutionObserver = EvolutionObserver.GetInstance(Parent);

            stateManager.SetGenes(geneIds);
            stateManager.PreviewLevel = 3;

            lock (_lockObject)
            {
                _blueprint = new BaseSolver(settings, stateManager, evolutionObserver);
                _stateManager = stateManager;
                _settings = settings;
                _evolutionObserver = evolutionObserver;
            }
        }

        public override void SetData(IGH_DataAccess DA)
        {
            string message;
            DA.SetData(0, "message");
        }

        public override WorkerInstance Duplicate()
        {
            return new WebSolverWorker(Parent);
        }
    }
}