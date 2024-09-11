using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Comonents;
using Newtonsoft.Json;
using Rhino.Compute;

namespace Geospiza.Core;

public class EvolutionObserver
{
    private static readonly Dictionary<GH_BasicSolver, EvolutionObserver> _instances = new();
    public int CurrentGenerationIndex { get; private set; }
    public Population CurrentPopulation { get; private set; }
    public List<double> AverageFitness { get; private set; } = new();
    public List<double> BestFitness { get; private set; } = new();
    public List<double> WorstFitness { get; private set; } = new();
    public List<double> TotalFitness { get; private set; } = new();
    public List<int> NumberOfUniqueIndividuals { get; private set; } = new();
    public List<int> Diversity { get; private set; }
    public List<Individual> BestIndividuals { get; private set; } = new();
    public List<double> FitnessStandardDeviation { get; private set; } = new();
    private static readonly object Padlock = new object();
    private readonly object listLock = new object();

    /// <summary>
    /// Returns the instance of Observer for the given solver.
    /// </summary>
    /// <param name="solver">The GH_BasicSolver for which the Observer instance is required.</param>
    /// <returns>The Observer instance associated with the given solver.</returns>
    public static EvolutionObserver GetInstance(GH_BasicSolver solver)
    {
        lock (Padlock)
        {
            if (!_instances.ContainsKey(solver))
            {
                _instances[solver] = new EvolutionObserver();
            }

            ComputeServer.ApiKey = "API";
            ComputeServer.AuthToken = "TOKEN";
            ComputeServer.WebAddress = "http://localhost:6500";
            var trees = new List<GrasshopperDataTree>();

            return _instances[solver];
        }
    }
    


    /// <summary>
    /// Creates a snapshot of the current population, recording various fitness metrics.
    /// </summary>
    /// <param name="currentPopulation">The current population to snapshot.</param>
    public void Snapshot(Population currentPopulation)
    {
        lock (listLock)
        {
            BestFitness ??= new List<double>();
            WorstFitness ??= new List<double>();
            AverageFitness ??= new List<double>();
            TotalFitness ??= new List<double>();
            NumberOfUniqueIndividuals ??= new List<int>();
            FitnessStandardDeviation ??= new List<double>();

            BestFitness.Add(currentPopulation.Inhabitants.Max(inh => inh.Fitness));
            WorstFitness.Add(currentPopulation.Inhabitants.Min(inh => inh.Fitness));
            AverageFitness.Add(currentPopulation.GetAverageFitness());
            TotalFitness.Add(currentPopulation.CalculateTotalFitness());
            NumberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());

            // Calculate standard deviation of fitness
            var average = currentPopulation.GetAverageFitness();
            var sumOfSquaresOfDifferences = currentPopulation.Inhabitants
                .Select(val => (val.Fitness - average) * (val.Fitness - average)).Sum();
            var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / currentPopulation.Inhabitants.Count);

            FitnessStandardDeviation.Add(standardDeviation);
        }
    }

    public void SetPopulation(Population population)
    {
        lock (listLock)
        {
            CurrentPopulation = population;
            var bestIndividual = CurrentPopulation.SelectTopIndividuals(1)[0];
            BestIndividuals.Add(bestIndividual);
        }
    }

    /// <summary>
    ///   Sets the new generation int
    /// </summary>
    public void UpdateGenerationCounter()
    {
        CurrentGenerationIndex++;
    }

    /// <summary>
    ///   Reset the observer
    /// </summary>
    public void Reset()
    {
        lock (listLock)
        {
            AverageFitness = new List<double>();
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
            BestIndividuals = new List<Individual>();
            BestFitness = new List<double>();
            WorstFitness = new List<double>();
            TotalFitness = new List<double>();
            NumberOfUniqueIndividuals = new List<int>();
            Diversity = new List<int>();
        }
    }

    /// <summary>
    /// Destroys all instances of the Observer by setting them to null.
    /// </summary>
    public void Destroy()
    {
        lock (Padlock)
        {
            var keys = new List<GH_BasicSolver>(_instances.Keys);
            foreach (var key in keys)
            {
                _instances[key] = null;
            }
        }
    }

    /// <summary>
    ///   Gets the current population
    /// </summary>
    /// <returns></returns>
    public Population GetCurrentPopulation()
    {
        return CurrentPopulation;
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public static EvolutionObserver FromJson(string json)
    {
        return JsonConvert.DeserializeObject<EvolutionObserver>(json);
    }
}