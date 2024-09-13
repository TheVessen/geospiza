using System;
using System.Collections.Concurrent;
using GeospizaManager.Core;
using GeospizaManager.GeospizaCordinator;
using GeospizaManager.Utils;
using GH_IO.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhino.Compute;

namespace GeospizaManager.GeospizaCordinator
{
    public class EvolutionarySolverCoordinator
    {
        private bool _comuteInitialized = false;
        private static EvolutionarySolverCoordinator _instance;
        private static readonly object Padlock = new object();
        private int _numberOfSolvers;
        private string _ghFilePath;
        private HttpServer _httpServer;
        private ConcurrentBag<ReducedObserver> _receivedObserver = new ConcurrentBag<ReducedObserver>();
        private TaskCompletionSource<bool> _allObserversReceived = new TaskCompletionSource<bool>();

        private ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        public event Action<string> DataProcessed;

        private EvolutionarySolverCoordinator(string ghFilePath, int numberOfSolvers)
        {
            _ghFilePath = ghFilePath;
            _numberOfSolvers = numberOfSolvers;
            _httpServer = HttpServer.Instance;
        }

        public static EvolutionarySolverCoordinator Instance(string ghFilePath, int numberOfSolvers)
        {
            lock (Padlock)
            {
                if (_instance == null)
                {
                    _instance = new EvolutionarySolverCoordinator(ghFilePath, numberOfSolvers);
                }

                return _instance;
            }
        }

        /// ////////////////////////////////////////////
        /// Compute
        /// ////////////////////////////////////////////
        public void InitCompute(string apiKey = "API", string authToken = "TOKEN",
            string webAddress = "http://localhost:6500")
        {
            ComputeServer.ApiKey = apiKey;
            ComputeServer.AuthToken = authToken;
            ComputeServer.WebAddress = webAddress;
            _comuteInitialized = true;
        }

        private List<GrasshopperDataTree> InitComputeRequest()
        {
            if (!_comuteInitialized)
            {
                throw new Exception("Compute not initialized");
            }

            var trees = new List<GrasshopperDataTree>();
            if (trees == null) throw new ArgumentNullException(nameof(trees));
            var random = new Random();
            var randomNumber = new GrasshopperObject(random.Next(0, 1000));
            var trigger = new GrasshopperDataTree("trigger");
            trigger.Add("0", new List<GrasshopperObject> { randomNumber });
            trees.Add(trigger);
            return trees;
        }

        private void StartComputeSolver()
        {
            var trees = InitComputeRequest();
            GrasshopperCompute.EvaluateDefinition(_ghFilePath, trees);
        }

        /// ////////////////////////////////////////////
        /// HTTP Server
        /// ////////////////////////////////////////////
        public async Task StartHttpServer()
        {
            await _httpServer.StartAsync($"{SharedVars.RootURL}/", OnDataReceived);
        }

        private async Task<string> OnDataReceived(string json)
        {
            try
            {
                Console.WriteLine("Write Data");
                var observerString = ReducedObserver.FromJson(json);
                _receivedObserver.Add(observerString);
                Console.WriteLine($"Received observer: {_receivedObserver.Count}");

                var requestId = observerString.RequestId;

                if (_receivedObserver.Count >= 2) // Adjust this condition as needed
                {
                    List<ReducedObserver> observers;
                    lock (_receivedObserver)
                    {
                        observers = _receivedObserver.ToList();
                        _receivedObserver.Clear();
                    }

                    var result = ProcessData(observers);
                    DataProcessed?.Invoke(result);

                    // Find and complete all pending requests
                    foreach (var observer in observers)
                    {
                        if (_pendingRequests.TryRemove(observer.RequestId, out var tcs))
                        {
                            tcs.TrySetResult(result);
                        }
                    }

                    Console.WriteLine("All observers received and processed.");
                    return result;
                }
                else
                {
                    // Create a new TaskCompletionSource for this request if it doesn't exist
                    var tcs = _pendingRequests.GetOrAdd(requestId, _ => new TaskCompletionSource<string>());
                    Console.WriteLine("Request added to pending list.");
                    // Wait for the result, but with a timeout
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2)); // Adjust timeout as needed
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        _pendingRequests.TryRemove(requestId, out _);
                        return "Timeout waiting for all observers.";
                    }
                    
                    _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

                    return await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Processes the accumulated data.
        /// </summary>
        /// <param name="observer">The list of received populations.</param>
        /// <returns>The result of the processing.</returns>
        private string ProcessData(List<ReducedObserver> observer)
        {
            return "Processing data...";
        }

        public async Task StopHttpServer()
        {
            await _httpServer.StopAsync();
        }
    }
}