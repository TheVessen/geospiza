using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Accord.IO;
using Fleck;
using GeospizaCore.Core;
using GeospizaCore.Solvers;
using GeospizaPlugin.AsyncComponent;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.Components.Solvers
{
    public class GH_WebSolver : GH_AsyncComponent
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
        protected override Bitmap Icon => Properties.Resources.Solver;

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

            var updateParam = new Param_Integer();
            updateParam.AddNamedValue("All", 0);
            updateParam.AddNamedValue("Every Generation", 1);
            updateParam.AddNamedValue("If Better", 2);
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
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Message",
                "M",
                "Status or results",
                GH_ParamAccess.item
            );
        }

        private class WebSolverWorker : WorkerInstance
        {
            private readonly object _lockObject = new object();
            private List<string> _geneIds;
            private SolverSettings _settings;
            private int _previewLevel;
            private bool _isActivated;
            private string _endpoint;
            private EvolutionObserver _evolutionObserver;
            private StateManager _stateManager;
            private bool _isRunning;
            private bool _webTriggerPending;
            private static WebSocketServer _wsServer;
            private static IWebSocketConnection _currentSocket;
            private static bool _serverStarted;

            public WebSolverWorker(GH_Component parent) : base(parent)
            {
            }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                // If cancellation is requested, just bail out.
                // if (CancellationToken.IsCancellationRequested)
                // {
                //     Done();
                //     return;
                // }
            
                _stateManager.SetGenes(_geneIds);
                _stateManager.PreviewLevel = _previewLevel;

                var _do = Parent.OnPingDocument();
                // bool shouldRun;
                // lock (_lockObject)
                // {
                //     shouldRun = _webTriggerPending && _isActivated && !_isRunning;
                //     if (shouldRun)
                //     {
                //     }
                // }
                //
                // if (shouldRun)
                // {
                //     ReportProgress("Running WebSocket triggered solver", 0);
                //     Parent.OnPingDocument().ScheduleSolution(100, ScheduleCallback);
                // }


                Done();
            }

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                var geneIds = new List<string>();
                if (!DA.GetDataList(0, geneIds)) return;

                var settings = new SolverSettings();
                if (!DA.GetData(1, ref settings)) return;

                var previewLevel = 0;
                if (!DA.GetData(2, ref previewLevel)) return;

                bool activated = false;
                if (!DA.GetData(3, ref activated)) return;

                string endpoint = "ws://127.0.0.1:8181";
                if (!DA.GetData(4, ref endpoint)) return;

                var stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
                var evolutionObserver = EvolutionObserver.GetInstance(Parent);

                lock (_lockObject)
                {
                    _geneIds = geneIds;
                    _settings = settings;
                    _previewLevel = previewLevel;
                    _isActivated = activated;
                    _endpoint = endpoint;
                    _stateManager = stateManager;
                    _evolutionObserver = evolutionObserver;
                }

                if (activated && !_serverStarted)
                {
                    StartWebSocketServer();
                }
            }

            private void StartWebSocketServer()
            {
                lock (_lockObject)
                {
                    if (_serverStarted) return;

                    _wsServer = new WebSocketServer(_endpoint);
                    _wsServer.Start(socket =>
                    {
                        socket.OnOpen = () => _currentSocket = socket;
                        socket.OnClose = () =>
                        {
                            if (_currentSocket == socket) _currentSocket = null;
                        };
                        socket.OnMessage = message =>
                        {
                            var msg = JsonSerializer.Deserialize<WebSocketMessage>(message);
                            RhinoApp.WriteLine($"Received WebSocket message: {msg.Command}");

                            var doc = Parent.OnPingDocument();

                            if (msg.Command?.ToLowerInvariant() == "run")
                            {
                                lock (_lockObject)
                                {
                                    _webTriggerPending = true;
                                }

                                if (doc != null)
                                {
                                    doc.ScheduleSolution(100, ScheduleCallback);
                                }
                            }
                        };
                    });
                    _serverStarted = true;
                }
            }

            private void ScheduleCallback(GH_Document doc)
            {
                lock (_lockObject)
                {
                    if (_isRunning) return;
                    _isRunning = true;
                }

                try
                {
                    EvolutionObserver observer;
                    SolverSettings settings;
                    StateManager sm;

                    lock (_lockObject)
                    {
                        observer = _evolutionObserver;
                        settings = _settings;
                        sm = _stateManager;
                        _webTriggerPending = false;
                    }

                    observer.Reset();
                    var solver = new BaseSolver(settings, sm, observer);
                    solver.RunAlgorithm();
                }
                finally
                {
                    lock (_lockObject)
                    {
                        _isRunning = false;
                    }

                    doc.ScheduleSolution(100, d => d.NewSolution(true));
                }
            }


            public override void SetData(IGH_DataAccess DA)
            {
                string message;
                lock (_lockObject)
                {
                    message = _isRunning ? "Solver running..." :
                        _isActivated ? "Listening for WebSocket commands" :
                        "Inactive - set Activate to true to enable";
                }

                DA.SetData(0, message);
            }

            public override WorkerInstance Duplicate() => new WebSolverWorker(Parent);
        }
    }
}


public class WebSocketMessage
{
    [JsonPropertyName("command")] public string Command { get; set; }

    [JsonPropertyName("payload")] public object Payload { get; set; }
}

public class SolverResult
{
    [JsonPropertyName("bestFitness")] public double BestFitness { get; set; }

    [JsonPropertyName("executionTime")] public double ExecutionTime { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("solution")] public object Solution { get; set; }
}