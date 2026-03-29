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
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.Components.Solvers;

/// <summary>
///     Streams live evolution data over WebSocket to a web client.
///     Wire the Run output into a solver's Run input to control which solver is targeted.
///     The observer discovers the connected solver from the wire and auto-subscribes to its
///     GenerationCompleted event when a run starts.
/// </summary>
public class GH_WebObserver : GH_Component
{
    private bool _activate;
    private EvolutionObserver _attachedObserver;
    private string _endpoint = "ws://127.0.0.1:8181";
    private bool _run;
    private bool _cancel;
    private WebSocketService _wsService;
    private List<WebIndividual> _cachedIndividuals = new();
    private List<WebIndividual> _lastSentIndividuals = null;

    public GH_WebObserver()
        : base("Web Observer", "WO",
            "Streams live evolution data over WebSocket. Wire Run → solver Run input to control which solver is targeted.",
            "Geospiza", "Solvers")
    {
    }

    public override Guid ComponentGuid => new("a3f1c2d4-8e5b-4a7f-9c6e-1b2d3e4f5a6b");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Resources.Solver;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("Activate", "A",
            "If TRUE, starts the WebSocket server and listens for run/cancel commands.",
            GH_ParamAccess.item, false);
        pManager.AddTextParameter("Endpoint", "E", "WebSocket endpoint to listen on.",
            GH_ParamAccess.item, "ws://127.0.0.1:8181");
        pManager.AddGenericParameter("WebIndividual", "WI",
            "Optional geometry individuals to stream to the web client.",
            GH_ParamAccess.list);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Message", "M", "Status message.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Run", "R",
            "Wire this into the solver's Run input. Flips to true when the web client sends {command:'run'}, false when done or canceled.",
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

        if (!activate)
        {
            StopServer();
            DA.SetData(0, "Inactive.");
            DA.SetData(1, false);
            return;
        }

        var individuals = new List<WebIndividual>();
        var ghObjects = new List<IGH_Goo>();
        DA.GetDataList(2, ghObjects);
        foreach (var goo in ghObjects)
            if (goo?.ScriptVariable() is WebIndividual wi)
                individuals.Add(wi);
        _cachedIndividuals = individuals;

        StartServer();

        DA.SetData(0, _wsService?.IsRunning == true ? $"Listening on {_endpoint}" : "Server failed to start.");
        DA.SetData(1, _run);
    }

    private void StartServer()
    {
        if (_wsService != null && _wsService.IsRunning)
            return;

        if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid WebSocket endpoint: {_endpoint}");
            return;
        }

        _wsService = new WebSocketService(_endpoint);
        _wsService.OnMessageReceived += HandleMessage;
        _wsService.OnClientConnected += OnClientConnected;
        _wsService.OnClientDisconnected += () => RhinoApp.WriteLine("WebObserver: client disconnected.");
        _wsService.Start();

        RhinoApp.WriteLine($"WebObserver: server started on {_endpoint}");
    }

    private void StopServer()
    {
        DetachObserver();
        _wsService?.Stop();
        _wsService = null;
    }

    /// <summary>
    ///     Finds the solver component connected to this observer's Run output (index 1)
    ///     and returns its EvolutionObserver singleton.
    /// </summary>
    private EvolutionObserver FindConnectedObserver()
    {
        var runOutput = Params.Output[1];
        var recipient = runOutput.Recipients.FirstOrDefault();
        if (recipient == null)
            return null;

        var solverComponent = recipient.Attributes?.GetTopLevel?.DocObject as GH_Component;
        if (solverComponent == null)
            return null;

        return EvolutionObserver.GetInstance(solverComponent);
    }

    private void AttachObserver(EvolutionObserver observer)
    {
        if (_attachedObserver == observer)
            return;

        DetachObserver();
        _attachedObserver = observer;
        _attachedObserver.GenerationCompleted += OnGenerationCompleted;
        _attachedObserver.RunCompleted += OnRunCompleted;
    }

    private void DetachObserver()
    {
        if (_attachedObserver == null)
            return;

        _attachedObserver.GenerationCompleted -= OnGenerationCompleted;
        _attachedObserver.RunCompleted -= OnRunCompleted;
        _attachedObserver = null;
    }

    private void OnGenerationCompleted(object sender, EvolutionObserver.GenerationCompletedEventArgs e)
    {
        if (_wsService == null || !_wsService.IsRunning)
            return;

        var observer = (EvolutionObserver)sender;
        _wsService.SendMessage(BuildMessage(observer, "running"));
    }

    private void OnRunCompleted(object sender, EventArgs e)
    {
        var observer = (EvolutionObserver)sender;
        _wsService?.SendMessage(BuildMessage(observer, "done"));
        _run = false;
        DetachObserver();
        OnPingDocument()?.ScheduleSolution(50, doc => ExpireSolution(true));
    }

    private void HandleMessage(string message)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(message);
            var command = doc.RootElement.TryGetProperty("command", out var cmd)
                ? cmd.GetString()?.ToLowerInvariant()
                : null;

            if (command == "run")
            {
                RhinoApp.InvokeOnUiThread(() => TriggerRun());
            }
            else if (command == "cancel")
            {
                RhinoApp.InvokeOnUiThread(() => TriggerCancel());
            }
            else if (command == "status")
            {
                var observer = FindConnectedObserver();
                var isRunning = observer != null && StateManager.GetRunningInstances().Count > 0;
                _wsService?.SendMessage(JsonSerializer.Serialize(new { status = isRunning ? "running" : "idle" }));
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"WebObserver message error: {ex.Message}");
        }
    }

    private void TriggerRun()
    {
        if (!_activate) return;

        var observer = FindConnectedObserver();
        if (observer != null)
            AttachObserver(observer);

        _run = true;
        _cancel = false;
        _lastSentIndividuals = null;

        OnPingDocument()?.ScheduleSolution(50, doc => ExpireSolution(true));
    }

    private void TriggerCancel()
    {
        _run = false;

        // Cancel via the StateManager's CTS directly — no need to wait for a solve cycle
        var running = StateManager.GetRunningInstances();
        foreach (var sm in running)
            sm.RunCts?.Cancel();

        _wsService?.SendMessage(JsonSerializer.Serialize(new { status = "canceled" }));

        DetachObserver();
        OnPingDocument()?.ScheduleSolution(50, doc => ExpireSolution(true));
    }


    private void OnClientConnected()
    {
        RhinoApp.WriteLine("WebObserver: client connected.");

        var observer = FindConnectedObserver();
        var algorithmType = observer?.Algorithm.ToString() ?? "Unknown";
        var objectiveNames = observer?.ObjectiveNames;

        var handshake = new Dictionary<string, object>
        {
            { "type", "handshake" },
            { "algorithm", algorithmType },
        };

        if (objectiveNames != null)
            handshake["objectiveNames"] = objectiveNames;

        _wsService?.SendMessage(JsonSerializer.Serialize(handshake,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
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
            { "currentGeneration", observer.CurrentGenerationIndex },
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
                    var objArray = new List<Dictionary<string, object>>();
                    for (var i = 0; i < latestStats.Length; i++)
                    {
                        var stat = latestStats[i];
                        var entry = new Dictionary<string, object>
                        {
                            { "name", names != null && i < names.Length ? names[i] : $"Objective {i + 1}" },
                            { "min", stat[0] },
                            { "max", stat[1] },
                            { "mean", stat[2] }
                        };
                        objArray.Add(entry);
                    }
                    root["objectives"] = objArray;
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
        StopServer();
        base.RemovedFromDocument(document);
    }
}
