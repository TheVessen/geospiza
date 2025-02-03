using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Fleck;
using GeospizaCore.Core;
using GeospizaCore.Web;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GeospizaPlugin.AsyncComponent;

/// <summary>
///     Base component providing asynchronous execution and WebSocket communication support.
/// </summary>
public abstract class GH_WebsocketComponent : GH_Component
{
    #region Constructor

    protected GH_WebsocketComponent(string name, string nickname, string description, string category,
        string subCategory)
        : base(name, nickname, description, category, subCategory)
    {
        // Set up the timer to periodically update progress.


        // Lambda for progress reporting.
        _reportProgress = (id, value) => { ProgressReports[id] = value; };

        // Lambda to be called when a worker is finished.
        _done = () =>
        {
            Interlocked.Increment(ref _state);
            if (_state == Workers.Count && _setData == 0)
            {
                Interlocked.Exchange(ref _setData, 1);
                Workers.Reverse();
                RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
            }
        };
    }

    #endregion

    // Every derived component must override this property.
    public abstract override Guid ComponentGuid { get; }

    #region Timer and Progress Updates

    /// <summary>
    ///     Updates the component's message with current progress.
    /// </summary>
    public virtual void DisplayProgress(object sender, ElapsedEventArgs e)
    {
        if (Workers.Count == 0 || ProgressReports.Values.Count == 0)
            return;

        Message = Workers.Count == 1
            ? ProgressReports.Values.Last().ToString("0.00%")
            : (ProgressReports.Values.Sum() / Workers.Count).ToString("0.00%");

        RhinoApp.InvokeOnUiThread((Action)(() => OnDisplayExpired(true)));
    }

    #endregion

    #region Cancellation

    /// <summary>
    ///     Requests cancellation of any running tasks.
    /// </summary>
    public void RequestCancellation()
    {
        lock (ServerLock)
        {
            foreach (var source in CancellationSources) source.Cancel();

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            _tasks.Clear();

            Interlocked.Exchange(ref _state, 0);
            Interlocked.Exchange(ref _setData, 0);
            Message = "Cancelled";
            OnDisplayExpired(true);

            // Notify client of cancellation.
            if (_currentSocket != null && _currentSocket.IsAvailable)
                _currentSocket.Send(JsonSerializer.Serialize(new { status = "cancelled" }));
        }
    }

    #endregion

    #region WebSocket Server

    /// <summary>
    ///     Starts the WebSocket server and configures client event handlers.
    /// </summary>
    public void StartWebSocketServer(WorkerInstance workerInstance)
    {
        lock (ServerLock)
        {
            if (_serverStarted)
                return;

            try
            {
                if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var uri) ||
                    !uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
                {
                    RhinoApp.WriteLine($"Invalid WebSocket endpoint format: {_endpoint}");
                    return;
                }

                _wsServer = new WebSocketServer(_endpoint)
                {
                    RestartAfterListenError = true
                };

                _wsServer.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        lock (ServerLock)
                        {
                            _currentSocket = socket;
                            RhinoApp.WriteLine($"Client connected from {socket.ConnectionInfo.ClientIpAddress}");
                            socket.Send("Connected to server");
                        }
                    };

                    socket.OnClose = () =>
                    {
                        lock (ServerLock)
                        {
                            if (_currentSocket == socket)
                                _currentSocket = null;
                            RhinoApp.WriteLine("WebSocket client disconnected.");
                        }
                    };

                    socket.OnError = exception => { RhinoApp.WriteLine($"WebSocket error: {exception.Message}"); };

                    socket.OnMessage = message =>
                    {
                        RhinoApp.WriteLine("Message received: " + message);
                        var msg = JsonSerializer.Deserialize<WebSocketMessage>(message);
                        var command = msg?.Command?.ToLowerInvariant();

                        if (command == "run")
                        {
                            lock (ServerLock)
                            {
                                var stateManager = StateManager.GetInstance(this, OnPingDocument());
                                if (stateManager.IsRunning)
                                    return;

                                RhinoApp.WriteLine("Run command received.");
                                var tokenSource = new CancellationTokenSource();
                                CancellationSources.Add(tokenSource);

                                var currentRun = TaskCreationOptions.HasValue
                                    ? new Task(() => workerInstance.DoWork(_reportProgress, _done),
                                        tokenSource.Token, TaskCreationOptions.Value)
                                    : new Task(() => workerInstance.DoWork(_reportProgress, _done),
                                        tokenSource.Token);

                                currentRun.Start();
                            }
                        }
                        else if (command == "cancel")
                        {
                            var stateManager = StateManager.GetInstance(this, OnPingDocument());
                            if (!stateManager.IsRunning)
                                return;
                            //TODO: Currently not working needs fix
                            RequestCancellation();
                        }
                    };
                });

                RhinoApp.WriteLine($"WebSocket server started on {_endpoint}");
                _serverStarted = true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to start WebSocket server: {ex.Message}");
                _serverStarted = false;
                _wsServer = null;
            }
        }
    }

    #endregion

    #region Fields and Properties

    // Progress reporting delegates and collections
    private readonly Action<string, double> _reportProgress;

    protected readonly ConcurrentDictionary<string, double> ProgressReports = new();

    private readonly Action _done;

    // Timers and state flags
    private int _state;
    private int _setData;

    // Workers and tasks
    protected List<WorkerInstance> Workers = new();
    private readonly List<Task> _tasks = new();

    // WebSocket server management
    private static readonly object ServerLock = new();
    private static WebSocketServer _wsServer;
    protected static volatile IWebSocketConnection _currentSocket;
    private static volatile bool _serverStarted;
    private string _endpoint = "ws://127.0.0.1:8181";

    // Cancellation tokens
    public readonly List<CancellationTokenSource> CancellationSources = new();

    /// <summary>
    ///     Base worker instance; should be set in the derived component's constructor.
    /// </summary>
    public WorkerInstance BaseWorker { get; set; }

    // Optional individuals data to be sent to the client
    private List<WebIndividual> _individuals = new();

    /// <summary>
    ///     Optional task creation options.
    /// </summary>
    public TaskCreationOptions? TaskCreationOptions { get; set; } = null;

    #endregion

    #region Setup and Cleanup

    protected override void BeforeSolveInstance()
    {
        // If a solution is in progress, leave it intact.
        if (_state != 0 && _setData == 1)
            return;

        // Cancel any running tasks and clear all state.
        foreach (var source in CancellationSources) source.Cancel();

        CancellationSources.Clear();
        Workers.Clear();
        ProgressReports.Clear();
        _tasks.Clear();
        Interlocked.Exchange(ref _state, 0);
    }

    protected override void AfterSolveInstance()
    {
        var stateManager = StateManager.GetInstance(this, OnPingDocument());
        if (!stateManager.IsRunning)
            return;

        if (_currentSocket != null)
        {
            var observer = EvolutionObserver.GetInstance(this);
            var json = CollectClientData(observer, stateManager);
            _currentSocket.Send(json);
        }

        if (!stateManager.IsRunning)
        {
            Params.Output[1].ClearData();
            Params.Output[1].AddVolatileData(new GH_Path(0), 0, stateManager);
        }
    }

    /// <summary>
    ///     Cleans up the WebSocket connection and server.
    /// </summary>
    private void CleanupWebSocket()
    {
        lock (ServerLock)
        {
            if (_currentSocket != null)
                try
                {
                    _currentSocket.Close();
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error closing WebSocket connection: {ex.Message}");
                }
                finally
                {
                    _currentSocket = null;
                }

            if (_wsServer != null)
                try
                {
                    _wsServer.Dispose();
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error disposing WebSocket server: {ex.Message}");
                }
                finally
                {
                    _wsServer = null;
                    _serverStarted = false;
                }
        }
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        try
        {
            CleanupWebSocket();

            foreach (var source in CancellationSources)
                try
                {
                    source.Cancel();
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error cancelling task: {ex.Message}");
                }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            _tasks.Clear();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error during component removal cleanup: {ex.Message}");
        }
        finally
        {
            base.RemovedFromDocument(document);
        }
    }

    #endregion

    #region Data Collection and Solve Instance

    /// <summary>
    ///     Collects the data to be sent to the client.
    /// </summary>
    private string CollectClientData(EvolutionObserver observer, StateManager stateManager)
    {
        var currentFitness = stateManager?.GetFitness() ?? 0;
        var rootObject = new Dictionary<string, object>();

        if (_individuals.Any())
        {
            var meshes = _individuals.Select(individual => individual.ToAnonymousObject()).ToList();
            rootObject.Add("meshes", meshes);
        }

        rootObject.Add("fitness", currentFitness);
        rootObject.Add("currentGeneration", observer.CurrentGenerationIndex);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return JsonSerializer.Serialize(rootObject, options);
    }

    protected override void ExpireDownStreamObjects()
    {
        if (_setData == 1)
            base.ExpireDownStreamObjects();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // First pass: setup and configuration.
        if (_state == 0)
        {
            if (BaseWorker == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                return;
            }

            var currentWorker = BaseWorker.Duplicate();

            var activated = false;
            if (!DA.GetData(2, ref activated) || !activated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Component is not activated.");
                if (_currentSocket != null) CleanupWebSocket();
                return;
            }

            if (!DA.GetData(3, ref _endpoint) || string.IsNullOrEmpty(_endpoint))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Endpoint not provided.");
                return;
            }

            var individualWrappers = new List<GH_ObjectWrapper>();
            DA.GetDataList(4, individualWrappers);
            if (individualWrappers.Any())
                _individuals = individualWrappers
                    .Select(i => i.ScriptVariable() as WebIndividual)
                    .ToList();

            currentWorker.GetData(DA, Params);

            if (!_serverStarted || _wsServer == null || _currentSocket == null) StartWebSocketServer(currentWorker);

            return;
        }

        // Second pass: feed data to workers.
        if (_setData == 0)
            return;

        if (Workers.Count > 0)
        {
            Interlocked.Decrement(ref _state);
            Workers[_state].SetData(DA);
        }

        if (_state != 0)
            return;

        Workers.Clear();
        ProgressReports.Clear();
        _tasks.Clear();
        Interlocked.Exchange(ref _setData, 0);

        Message = "Done";
        OnDisplayExpired(true);
    }

    #endregion
}

/// <summary>
///     Represents a WebSocket message with a command and an optional payload.
/// </summary>
public class WebSocketMessage
{
    [JsonPropertyName("command")] public string Command { get; set; }

    [JsonPropertyName("payload")] public object Payload { get; set; }
}