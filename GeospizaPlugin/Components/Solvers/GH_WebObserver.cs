using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using GeospizaCore.Core;
using GeospizaCore.Web;
using GeospizaPlugin.AsyncComponent;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.Components.Solvers;

/// <summary>
///     Streams live evolution data over WebSocket.
///
///     LOCAL mode (default): hosts a WebSocket server. Wire Run output → solver Run input.
///     Web client connects and sends {"command":"run"} to start.
///
///     HEADLESS mode (RhinoCompute): detected automatically via RhinoApp.IsRunningHeadless.
///     Connects as a WebSocket client to the Endpoint input. Auto-starts the solver once
///     after connecting. The Endpoint should be set via the RhinoCompute HTTP request inputs.
/// </summary>
public class GH_WebObserver : GH_Component
{
    private bool _activate;
    private EvolutionObserver _attachedObserver;
    private string _endpoint = "ws://127.0.0.1:8181";
    private bool _run;
    private bool _headlessRunFired;

    // Local mode
    private WebSocketService _wsServer;

    // Headless mode
    private WebSocketClientService _wsClient;

    private List<WebIndividual> _cachedIndividuals = new();
    private List<WebIndividual> _lastSentIndividuals;

    public GH_WebObserver()
        : base("Web Observer", "WO",
            "Streams live evolution data over WebSocket. " +
            "Local: hosts server, wire Run → solver. " +
            "Headless (RhinoCompute): connects as client to Endpoint, auto-starts solver.",
            "Geospiza", "Solvers")
    {
    }

    public override Guid ComponentGuid => new("a3f1c2d4-8e5b-4a7f-9c6e-1b2d3e4f5a6b");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Resources.Solver;

    private bool IsHeadless => RhinoApp.IsRunningHeadless;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("Activate", "A",
            "Local mode: if TRUE starts the WebSocket server. Ignored in headless mode.",
            GH_ParamAccess.item, false);
        pManager.AddTextParameter("Endpoint", "E",
            "Local mode: address to host on.\nHeadless mode: address to connect to (set via RhinoCompute input).",
            GH_ParamAccess.item, "ws://127.0.0.1:8181");
        pManager.AddGenericParameter("WebIndividual", "WI",
            "Optional geometry to stream. Updated per PreviewLevel.",
            GH_ParamAccess.list);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Message", "M", "Status message.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Run", "R",
            "Wire into solver's Run input. " +
            "Local: flips true on {command:'run'} from client. " +
            "Headless: flips true once after WS connection is established.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var activate = false;
        DA.GetData(0, ref activate);

        var endpoint = "ws://127.0.0.1:8181";
        DA.GetData(1, ref endpoint);

        _activate = activate;
        _endpoint = endpoint;

        // Cache WebIndividual geometry every solve cycle
        var ghObjects = new List<IGH_Goo>();
        DA.GetDataList(2, ghObjects);
        var individuals = new List<WebIndividual>();
        foreach (var goo in ghObjects)
            if (goo?.ScriptVariable() is WebIndividual wi)
                individuals.Add(wi);
        _cachedIndividuals = individuals;

        if (IsHeadless)
        {
            HandleHeadlessSolve(DA);
        }
        else
        {
            HandleLocalSolve(DA, activate);
        }
    }

    // -------------------------------------------------------------------------
    // LOCAL MODE
    // -------------------------------------------------------------------------

    private void HandleLocalSolve(IGH_DataAccess DA, bool activate)
    {
        if (!activate)
        {
            StopLocalServer();
            DA.SetData(0, "Inactive.");
            DA.SetData(1, false);
            return;
        }

        StartLocalServer();
        DA.SetData(0, _wsServer?.IsRunning == true ? $"Listening on {_endpoint}" : "Server failed to start.");
        DA.SetData(1, _run);
    }

    private void StartLocalServer()
    {
        if (_wsServer != null && _wsServer.IsRunning)
            return;

        if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid WebSocket endpoint: {_endpoint}");
            return;
        }

        _wsServer = new WebSocketService(_endpoint);
        _wsServer.OnMessageReceived += HandleIncomingMessage;
        _wsServer.OnClientConnected += SendHandshake;
        _wsServer.OnClientDisconnected += () => RhinoApp.WriteLine("WebObserver: client disconnected.");
        _wsServer.Start();

        RhinoApp.WriteLine($"WebObserver: server started on {_endpoint}");
    }

    private void StopLocalServer()
    {
        DetachObserver();
        _wsServer?.Stop();
        _wsServer = null;
    }

    private void SendMessage(string json)
    {
        if (IsHeadless)
            _wsClient?.SendMessage(json);
        else
            _wsServer?.SendMessage(json);
    }

    // -------------------------------------------------------------------------
    // HEADLESS MODE
    // -------------------------------------------------------------------------

    private void HandleHeadlessSolve(IGH_DataAccess DA)
    {
        // Connect once
        if (_wsClient == null || !_wsClient.IsConnected)
            ConnectAsClient();

        // Auto-trigger run once after connecting, but never re-trigger
        if (_wsClient?.IsConnected == true && !_headlessRunFired && !_run)
        {
            _headlessRunFired = true;
            TriggerRun();
        }

        var status = _wsClient?.IsConnected == true ? $"Connected to {_endpoint}" : $"Connecting to {_endpoint}...";
        DA.SetData(0, status);
        DA.SetData(1, _run);
    }

    private void ConnectAsClient()
    {
        _wsClient?.Dispose();
        _wsClient = new WebSocketClientService(_endpoint);
        _wsClient.OnConnected += () =>
        {
            RhinoApp.WriteLine("WebObserver: connected to server.");
            SendHandshake();
        };
        _wsClient.OnDisconnected += () => RhinoApp.WriteLine("WebObserver: disconnected from server.");
        _wsClient.OnMessageReceived += HandleIncomingMessage;

        // Block briefly to establish connection before first solve
        _wsClient.ConnectAsync().GetAwaiter().GetResult();
    }

    // -------------------------------------------------------------------------
    // SHARED
    // -------------------------------------------------------------------------

    private void SendHandshake()
    {
        var observer = FindConnectedObserver();
        var handshake = new Dictionary<string, object>
        {
            { "type", "handshake" },
            { "algorithm", observer?.Algorithm.ToString() ?? "Unknown" },
            { "mode", IsHeadless ? "headless" : "local" }
        };

        if (observer?.ObjectiveNames != null)
            handshake["objectiveNames"] = observer.ObjectiveNames;

        SendMessage(JsonSerializer.Serialize(handshake,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private void HandleIncomingMessage(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var command = doc.RootElement.TryGetProperty("command", out var cmd)
                ? cmd.GetString()?.ToLowerInvariant()
                : null;

            if (command == "run")
                RhinoApp.InvokeOnUiThread(() => TriggerRun());
            else if (command == "cancel")
                RhinoApp.InvokeOnUiThread(() => TriggerCancel());
            else if (command == "status")
            {
                var isRunning = StateManager.GetRunningInstances().Count > 0;
                SendMessage(JsonSerializer.Serialize(new { status = isRunning ? "running" : "idle" }));
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"WebObserver message error: {ex.Message}");
        }
    }

    private EvolutionObserver FindConnectedObserver()
    {
        var recipient = Params.Output[1].Recipients.FirstOrDefault();
        if (recipient == null) return null;
        var solverComponent = recipient.Attributes?.GetTopLevel?.DocObject as GH_Component;
        return solverComponent == null ? null : EvolutionObserver.GetInstance(solverComponent);
    }

    private void AttachObserver(EvolutionObserver observer)
    {
        if (_attachedObserver == observer) return;
        DetachObserver();
        _attachedObserver = observer;
        _attachedObserver.GenerationCompleted += OnGenerationCompleted;
        _attachedObserver.RunCompleted += OnRunCompleted;
    }

    private void DetachObserver()
    {
        if (_attachedObserver == null) return;
        _attachedObserver.GenerationCompleted -= OnGenerationCompleted;
        _attachedObserver.RunCompleted -= OnRunCompleted;
        _attachedObserver = null;
    }

    private void TriggerRun()
    {
        if (!IsHeadless && !_activate) return;

        var observer = FindConnectedObserver();
        if (observer != null) AttachObserver(observer);

        _run = true;
        _lastSentIndividuals = null;

        OnPingDocument()?.ScheduleSolution(50, doc => ExpireSolution(true));
    }

    private void TriggerCancel()
    {
        _run = false;

        var running = StateManager.GetRunningInstances();
        foreach (var sm in running)
            sm.RunCts?.Cancel();

        SendMessage(JsonSerializer.Serialize(new { status = "canceled" }));
        DetachObserver();
        OnPingDocument()?.ScheduleSolution(50, doc => ExpireSolution(true));
    }

    private void OnGenerationCompleted(object sender, EvolutionObserver.GenerationCompletedEventArgs e)
    {
        SendMessage(BuildMessage((EvolutionObserver)sender, "running"));
    }

    private void OnRunCompleted(object sender, EventArgs e)
    {
        SendMessage(BuildMessage((EvolutionObserver)sender, "done"));
        _run = false;
        DetachObserver();

        if (IsHeadless)
        {
            // Disconnect cleanly — compute instance is done
            _wsClient?.Disconnect();
        }
        else
        {
            OnPingDocument()?.ScheduleSolution(50, doc => ExpireSolution(true));
        }
    }

    private string BuildMessage(EvolutionObserver observer, string status)
    {
        var individuals = _cachedIndividuals;
        var meshesUpdated = !ReferenceEquals(individuals, _lastSentIndividuals) && individuals.Count > 0;
        if (meshesUpdated) _lastSentIndividuals = individuals;

        var root = new Dictionary<string, object>
        {
            { "status", status },
            { "algorithm", observer.Algorithm.ToString() },
            { "currentGeneration", observer.CurrentGenerationIndex }
        };

        switch (observer.Algorithm)
        {
            case EvolutionObserver.AlgorithmType.SingleObjective:
                root["bestFitness"] = observer.BestFitness.Count > 0
                    ? observer.BestFitness[observer.BestFitness.Count - 1] : 0.0;
                root["averageFitness"] = observer.AverageFitness.Count > 0
                    ? observer.AverageFitness[observer.AverageFitness.Count - 1] : 0.0;
                root["worstFitness"] = observer.WorstFitness.Count > 0
                    ? observer.WorstFitness[observer.WorstFitness.Count - 1] : 0.0;
                break;

            case EvolutionObserver.AlgorithmType.NsgaII:
            case EvolutionObserver.AlgorithmType.NsgaIII:
                root["hypervolume"] = observer.Hypervolume.Count > 0
                    ? observer.Hypervolume[observer.Hypervolume.Count - 1] : 0.0;
                root["paretoFrontSize"] = observer.ParetoFrontSizes.Count > 0
                    ? observer.ParetoFrontSizes[observer.ParetoFrontSizes.Count - 1] : 0;
                root["frontCount"] = observer.FrontCount.Count > 0
                    ? observer.FrontCount[observer.FrontCount.Count - 1] : 0;

                if (observer.ObjectiveStats.Count > 0)
                {
                    var latestStats = observer.ObjectiveStats[observer.ObjectiveStats.Count - 1];
                    var names = observer.ObjectiveNames;
                    var objList = new List<Dictionary<string, object>>();
                    for (var i = 0; i < latestStats.Length; i++)
                    {
                        var stat = latestStats[i];
                        objList.Add(new Dictionary<string, object>
                        {
                            { "name", names != null && i < names.Length ? names[i] : $"Objective {i + 1}" },
                            { "min", stat[0] },
                            { "max", stat[1] },
                            { "mean", stat[2] }
                        });
                    }
                    root["objectives"] = objList;
                }
                break;
        }

        if (meshesUpdated)
            root["meshes"] = individuals.ConvertAll(wi => wi.ToAnonymousObject());

        return JsonSerializer.Serialize(root,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        StopLocalServer();
        _wsClient?.Dispose();
        base.RemovedFromDocument(document);
    }
}
