using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Timer = System.Timers.Timer;
using Fleck;
using Rhino;

namespace GeospizaPlugin.AsyncComponent
{
    /// <summary>
    /// Inherit your component from this class to make all the async goodness available.
    /// </summary>
    public abstract class GH_WebsocketComponent : GH_Component
    {
        public override Guid ComponentGuid =>
            throw new Exception("ComponentGuid should be overriden in any descendant of GH_AsyncComponent!");

        //List<(string, GH_RuntimeMessageLevel)> Errors;

        Action<string, double> ReportProgress;

        public ConcurrentDictionary<string, double> ProgressReports;

        Action Done;

        Timer DisplayProgressTimer;

        int State = 0;

        int SetData = 0;

        public List<WorkerInstance> Workers;

        List<Task> Tasks;

        private static readonly object _serverLock = new object();
        private static WebSocketServer _wsServer;
        private static volatile IWebSocketConnection _currentSocket;
        private static volatile bool _serverStarted;
        private string _endpoint = "ws://127.0.0.1:8181";
        private volatile bool _isRunning;
        public readonly List<CancellationTokenSource> CancellationSources;
        
        /// <summary>
        /// Set this property inside the constructor of your derived component. 
        /// </summary>
        public WorkerInstance BaseWorker { get; set; }

        /// <summary>
        /// Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
        /// </summary>
        public TaskCreationOptions? TaskCreationOptions { get; set; } = null;
        

        protected GH_WebsocketComponent(string name, string nickname, string description, string category,
            string subCategory) : base(name, nickname, description, category, subCategory)
        {
            DisplayProgressTimer = new Timer(333) { AutoReset = false };
            DisplayProgressTimer.Elapsed += DisplayProgress;

            ReportProgress = (id, value) =>
            {
                ProgressReports[id] = value;
                if (!DisplayProgressTimer.Enabled)
                {
                    DisplayProgressTimer.Start();
                }
            };

            Done = () =>
            {
                Interlocked.Increment(ref State);
                if (State == Workers.Count && SetData == 0)
                {
                    Interlocked.Exchange(ref SetData, 1);

                    Workers.Reverse();

                    Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
                    _isRunning = false;
                }
            };

            ProgressReports = new ConcurrentDictionary<string, double>();

            Workers = new List<WorkerInstance>();
            CancellationSources = new List<CancellationTokenSource>();
            Tasks = new List<Task>();
        }

        public virtual void DisplayProgress(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Workers.Count == 0 || ProgressReports.Values.Count == 0)
            {
                return;
            }

            if (Workers.Count == 1)
            {
                Message = ProgressReports.Values.Last().ToString("0.00%");
            }
            else
            {
                double total = 0;
                foreach (var kvp in ProgressReports)
                {
                    total += kvp.Value;
                }

                Message = (total / Workers.Count).ToString("0.00%");
            }

            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { OnDisplayExpired(true); });
        }

        public void setEndpoint(string endpoint)
        {
            _endpoint = endpoint;
        }

        protected override void BeforeSolveInstance()
        {
            if (State != 0 && SetData == 1)
            {
                return;
            }

            Debug.WriteLine("Killing");

            foreach (var source in CancellationSources)
            {
                source.Cancel();
            }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref State, 0);
        }

        protected override void AfterSolveInstance()
        {
            System.Diagnostics.Debug.WriteLine("After solve instance was called " + State + " ? " + Workers.Count);
            if (State == 0 && Tasks.Count > 0 && SetData == 0)
            {
                System.Diagnostics.Debug.WriteLine("After solve INVOKATIONM");
                foreach (var task in Tasks)
                {
                    task.Start();
                }
            }
        }

        protected override void ExpireDownStreamObjects()
        {
            if (SetData == 1)
            {
                base.ExpireDownStreamObjects();
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (State == 0)
            {
                if (BaseWorker == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                    return;
                }

                var currentWorker = BaseWorker.Duplicate();
                if (currentWorker == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get a worker instance.");
                    return;
                }

                currentWorker.GetData(DA, Params);

                if (!_serverStarted || _wsServer == null || _currentSocket == null)
                {
                    StartWebSocketServer(currentWorker);
                }

                return;
            }

            if (SetData == 0)
            {
                return;
            }

            if (Workers.Count > 0)
            {
                Interlocked.Decrement(ref State);
                Workers[State].SetData(DA);
            }

            if (State != 0)
            {
                return;
            }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref SetData, 0);

            Message = "Done";
            OnDisplayExpired(true);
        }

        public void RequestCancellation()
        {
            foreach (var source in CancellationSources)
            {
                source.Cancel();
            }

            CancellationSources.Clear();
            Workers.Clear();
            ProgressReports.Clear();
            Tasks.Clear();

            Interlocked.Exchange(ref State, 0);
            Interlocked.Exchange(ref SetData, 0);
            Message = "Cancelled";
            OnDisplayExpired(true);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            try
            {
                CleanupWebSocket();

                // Cancel any running tasks
                foreach (var source in CancellationSources)
                {
                    try
                    {
                        source.Cancel();
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Error cancelling task: {ex.Message}");
                    }
                }

                // Clear collections
                CancellationSources.Clear();
                Workers.Clear();
                ProgressReports.Clear();
                Tasks.Clear();

                _isRunning = false;
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
        
        private void CleanupWebSocket()
        {
            try
            {
                lock (_serverLock)
                {
                    // Close current connection
                    var socket = _currentSocket;
                    if (socket != null)
                    {
                        try
                        {
                            socket.Close();
                        }
                        catch (Exception ex)
                        {
                            RhinoApp.WriteLine($"Error closing WebSocket connection: {ex.Message}");
                        }
                        finally
                        {
                            _currentSocket = null;
                        }
                    }

                    // Dispose server
                    if (_wsServer != null)
                    {
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
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during WebSocket cleanup: {ex.Message}");
            }
        }



        public void StartWebSocketServer(WorkerInstance workerInstance)
        {
            lock (_serverLock)
            {
                if (_serverStarted)
                    return;

                try
                {
                    // Validate endpoint format
                    if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out Uri uri) ||
                        !uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
                    {
                        RhinoApp.WriteLine($"Invalid WebSocket endpoint format: {_endpoint}");
                        return;
                    }

                    _wsServer = new WebSocketServer(_endpoint);
                    _wsServer.RestartAfterListenError = true;

                    _wsServer.Start(socket =>
                    {
                        socket.OnOpen = () =>
                        {
                            lock (_serverLock)
                            {
                                _currentSocket = socket;
                                RhinoApp.WriteLine($"Client connected from {socket.ConnectionInfo.ClientIpAddress}");
                                socket.Send("Connected to server");
                            }
                        };

                        socket.OnClose = () =>
                        {
                            lock (_serverLock)
                            {
                                if (_currentSocket == socket)
                                    _currentSocket = null;
                                RhinoApp.WriteLine("WebSocket client disconnected.");
                            }
                        };

                        socket.OnError = (exception) =>
                        {
                            RhinoApp.WriteLine($"WebSocket error: {exception.Message}");
                        };

                        socket.OnMessage = message =>
                        {
                            RhinoApp.WriteLine("Message received: " + message);
                            var msg = JsonSerializer.Deserialize<WebSocketMessage>(message);
                            
                            if (msg?.Command?.ToLowerInvariant() == "run")
                            {
                                lock (_serverLock)
                                {
                                    if (_isRunning)
                                        return;
                            
                                    RhinoApp.WriteLine("Run command received.");
                                    var tokenSource = new CancellationTokenSource();
                                    var currentRun = TaskCreationOptions != null
                                        ? new Task(() => workerInstance.DoWork(ReportProgress, Done), tokenSource.Token,
                                            (TaskCreationOptions)TaskCreationOptions)
                                        : new Task(() => workerInstance.DoWork(ReportProgress, Done),
                                            tokenSource.Token);
                            
                                    CancellationSources.Add(tokenSource);
                                    currentRun.Start();
                                }
                            }
                            else if (msg?.Command?.ToLowerInvariant() == "cancel")
                            {
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
    }
}