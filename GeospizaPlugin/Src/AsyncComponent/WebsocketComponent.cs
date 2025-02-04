using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using GeospizaCore.Core;
using GeospizaCore.Web;
using GeospizaPlugin.Components.Solvers;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.AsyncComponent
{
    /// <summary>
    /// Base component providing asynchronous execution and WebSocket communication without progress reporting.
    /// </summary>
    public abstract class GH_WebsocketComponent : GH_Component
    {
        public abstract override Guid ComponentGuid { get; }
        private CancellationTokenSource _globalCancellationTokenSource = new CancellationTokenSource();
        protected WorkerInstance BaseWorker { get; set; }
        private string Endpoint { get; set; } = "ws://127.0.0.1:8181";
        protected WebSocketService _wsService;
        
        Action Done;

        protected GH_WebsocketComponent(string name, string nickname, string description, string category,
            string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
            Done = () =>
            {
                
                RhinoApp.InvokeOnUiThread(() => OnDisplayExpired(true));
                _wsService?.SendMessage(JsonSerializer.Serialize(new { status = "done" }));
            };

            
        }

        /// <summary>
        /// Starts the WebSocket server if not already running.
        /// </summary>
        private void StartWebSocketServer()
        {
            if (_wsService != null && _wsService.IsRunning)
                return;

            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) ||
                !uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
            {
                RhinoApp.WriteLine($"Invalid WebSocket endpoint format: {Endpoint}");
                return;
            }

            _wsService = new WebSocketService(Endpoint);
            _wsService.OnMessageReceived += HandleWebSocketMessage;
            _wsService.OnClientConnected += () => RhinoApp.WriteLine("Client connected.");
            _wsService.OnClientDisconnected += () => RhinoApp.WriteLine("Client disconnected.");
            _wsService.Start();
            RhinoApp.WriteLine($"WebSocket server started on {Endpoint}");
        }

        /// <summary>
        /// Handles incoming WebSocket messages.
        /// </summary>
        private void HandleWebSocketMessage(string message)
        {
            RhinoApp.WriteLine("Message received: " + message);
            try
            {
                var msg = JsonSerializer.Deserialize<WebSocketMessage>(message);
                string command = msg?.Command?.ToLowerInvariant();
                if (command == "run")
                {
                    var stateManager = StateManager.GetInstance(this, OnPingDocument());
                    if (stateManager.IsRunning)
                        return;

                    var tokenSource =
                        CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationTokenSource.Token);

                    if (BaseWorker == null)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                    }

                    Task.Run(() => BaseWorker.DoWork(
                        Done,
                        tokenSource.Token), tokenSource.Token);
                }
                else if (command == "cancel")
                {
                    RhinoApp.InvokeOnUiThread(() => RequestCancellation());
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error handling websocket message: {ex.Message}");
            }
        }

        /// <summary>
        /// Requests cancellation of running tasks.
        /// </summary>
        /// <summary>
        /// Requests cancellation of running tasks.
        /// </summary>
        public void RequestCancellation()
        {
            RhinoApp.WriteLine("Cancellation requested.");
            _globalCancellationTokenSource.Cancel();
            _wsService?.SendMessage(JsonSerializer.Serialize(new { status = "canceled" }));

            // Reset the CancellationTokenSource
            _globalCancellationTokenSource = new CancellationTokenSource();
        }


        protected override void BeforeSolveInstance()
        {
            // Optionally cancel or clear state here.
        }

        protected override void AfterSolveInstance()
        {
            var stateManager = StateManager.GetInstance(this, OnPingDocument());

            if (stateManager.IsRunning && _wsService != null)
            {
                var observer = EvolutionObserver.GetInstance(this);
                string json = CollectClientData(observer, stateManager);
                _wsService.SendMessage(json);
                Params.Output[0].ClearData();
                Params.Output[0].AddVolatileData(new GH_Path(0), 0, new GH_String("Running..."));
            }
            else if (stateManager.IsRunning == false)
            {
                Params.Output[0].ClearData();
                var observer = EvolutionObserver.GetInstance(this);
                Params.Output[1].AddVolatileData(new GH_Path(0), 0, observer);
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (BaseWorker == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                return;
            }

            bool activate = false;
            if (!DA.GetData(2, ref activate))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Activation flag not provided.");
                return;
            }

            if (!activate)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Component is not activated.");
                _wsService?.Stop();
                return;
            }

            string endpointInput = null;
            if (DA.GetData(3, ref endpointInput) && !string.IsNullOrEmpty(endpointInput))
                Endpoint = endpointInput;
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Endpoint not provided.");
                return;
            }

            // var worker = BaseWorker.Duplicate();
            BaseWorker.GetData(DA);
            BaseWorker = BaseWorker;

            StartWebSocketServer();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            try
            {
                _wsService?.Stop();
                _globalCancellationTokenSource.Cancel();
                BaseWorker = null;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during removal cleanup: {ex.Message}");
            }
            finally
            {
                base.RemovedFromDocument(document);
            }
        }

        /// <summary>
        /// Collects data to be sent to the client.
        /// </summary>
        protected string CollectClientData(EvolutionObserver observer, StateManager stateManager)
        {
            var structEnum = Params.Input[4].VolatileData.AllData(true).ToList();
            var individuals = structEnum.Select(goo => goo.ScriptVariable() as WebIndividual).ToList();

            var rootObject = new Dictionary<string, object>
            {
                { "fitness", stateManager?.GetFitness() ?? 0 },
                { "currentGeneration", observer?.CurrentGenerationIndex ?? 0 },
                {"status", stateManager?.IsRunning ?? false ? "running" : "finished"}
            };

            if (individuals.Any())
            {
                var meshes = individuals.Select(individual => individual.ToAnonymousObject()).ToList();
                rootObject.Add("meshes", meshes);
            }

            return JsonSerializer.Serialize(rootObject,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        }
    }

    /// <summary>
    /// Represents a WebSocket message with a command and an optional payload.
    /// </summary>
    public class WebSocketMessage
    {
        [JsonPropertyName("command")] public string Command { get; set; }

        [JsonPropertyName("payload")] public object Payload { get; set; }
    }
}