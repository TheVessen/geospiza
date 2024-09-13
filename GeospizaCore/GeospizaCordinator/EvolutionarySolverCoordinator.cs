using System;
using System.Collections.Concurrent;
using GeospizaManager.Core;
using GeospizaManager.GeospizaCordinator;
using GH_IO.Serialization;
using Newtonsoft.Json;
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
        private ConcurrentBag<Individual> _receivedPopulations = new ConcurrentBag<Individual>();
        private TaskCompletionSource<bool> _allDataReceived = new TaskCompletionSource<bool>();
        public event Action<string> DataProcessed;

        private EvolutionarySolverCoordinator(string ghFilePath, int numberOfSolvers)
        {
            _ghFilePath = ghFilePath;
            _numberOfSolvers = numberOfSolvers;
            _httpServer = HttpServer.Instance;
        }
        
        public void InitCompute(string apiKey ="API", string authToken = "TOKEN", string webAddress = "http://localhost:6500")
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

        public async Task StartHttpServer()
        {
            await _httpServer.StartAsync("http://localhost:8080/", OnDataReceived);
        }

        private async Task<string> OnDataReceived(string json)
        {
          Console.WriteLine("OnDataReceived called with JSON: " + json);
          try
          {
            var pop = Individual.FromJson(json);
            _receivedPopulations.Add(pop);
            Console.WriteLine("Population added. Current count: " + _receivedPopulations.Count);

            // Temporarily adjust the condition for testing
            if (_receivedPopulations.Count >= 1) // Change this back to _numberOfSolvers for actual use
            {
              // Process the accumulated data
              var result = ProcessData(_receivedPopulations.ToList());
              Console.WriteLine("Data processed. Result: " + result);

              // Notify subscribers with the result
              DataProcessed?.Invoke(result);

              // Signal that all data has been received and processed
              if (!_allDataReceived.Task.IsCompleted)
              {
                _allDataReceived.SetResult(true);
              }

              return result; // Return the result
            }

            return "Data received but not yet processed.";
          }
          catch (Exception ex)
          {
            Console.WriteLine($"Error processing received data: {ex.Message}");
            return $"Error: {ex.Message}"; // Return the error message
          }
        }
        
        /// <summary>
        /// Processes the accumulated data.
        /// </summary>
        /// <param name="populations">The list of received populations.</param>
        /// <returns>The result of the processing.</returns>
        private string ProcessData(List<Individual> populations)
        {
            // Implement your data processing logic here
            // For example, you can calculate the average fitness of all individuals
            double totalFitness = populations.Sum(ind => ind.Fitness);
            double averageFitness = totalFitness / populations.Count;

            return $"Average Fitness: {averageFitness}";
        }
        
        public async Task StopHttpServer()
        {
            await _httpServer.StopAsync();
        }

        public static bool IsInstantiated()
        {
            return _instance != null;
        }
    }
}