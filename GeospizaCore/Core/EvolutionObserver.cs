using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GeospizaManager.Utils;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GeospizaManager.Core;

public class EvolutionObserver : IDisposable
{
    private static readonly ConcurrentDictionary<GH_Component, EvolutionObserver> _instances = new();
    private readonly object _listLock = new();
    
    public int CurrentGenerationIndex { get; private set; }
    public Population CurrentPopulation { get; private set; }
    public IReadOnlyList<double> AverageFitness => _averageFitness;
    public IReadOnlyList<double> BestFitness => _bestFitness;
    public IReadOnlyList<double> WorstFitness => _worstFitness;
    public IReadOnlyList<double> TotalFitness => _totalFitness;
    public IReadOnlyList<int> NumberOfUniqueIndividuals => _numberOfUniqueIndividuals;
    public IReadOnlyList<int> Diversity => _diversity;
    public IReadOnlyList<Individual> BestIndividuals => _bestIndividuals;
    public IReadOnlyList<double> FitnessStandardDeviation => _fitnessStandardDeviation;

    private readonly List<double> _averageFitness = new();
    private readonly List<double> _bestFitness = new();
    private readonly List<double> _worstFitness = new();
    private readonly List<double> _totalFitness = new();
    private readonly List<int> _numberOfUniqueIndividuals = new();
    private readonly List<int> _diversity = new();
    private readonly List<Individual> _bestIndividuals = new();
    private readonly List<double> _fitnessStandardDeviation = new();
    private bool _isDisposed;

    private EvolutionObserver() { }

    public static EvolutionObserver GetInstance(GH_Component solver)
    {
        ArgumentNullException.ThrowIfNull(solver);
        return _instances.GetOrAdd(solver, _ => new EvolutionObserver());
    }

    public void Snapshot(Population currentPopulation)
    {
        ArgumentNullException.ThrowIfNull(currentPopulation);
        
        lock (_listLock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));

            var inhabitants = currentPopulation.Inhabitants;
            _bestFitness.Add(inhabitants.Max(inh => inh.Fitness));
            _worstFitness.Add(inhabitants.Min(inh => inh.Fitness));
            _averageFitness.Add(currentPopulation.GetAverageFitness());
            _totalFitness.Add(currentPopulation.CalculateTotalFitness());
            _numberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());

            double standardDeviation = CalculateStandardDeviation(inhabitants);
            _fitnessStandardDeviation.Add(standardDeviation);
        }
    }

    private static double CalculateStandardDeviation(IEnumerable<Individual> inhabitants)
    {
        var fitnessList = inhabitants.Select(i => i.Fitness).ToList();
        double average = fitnessList.Average();
        double sumOfSquaresOfDifferences = fitnessList.Sum(val => Math.Pow(val - average, 2));
        return Math.Sqrt(sumOfSquaresOfDifferences / fitnessList.Count);
    }

    public void SetPopulation(Population population)
    {
        ArgumentNullException.ThrowIfNull(population);
        
        lock (_listLock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));
            
            CurrentPopulation = population;
            var bestIndividual = population.SelectTopIndividuals(1).FirstOrDefault() 
                ?? throw new InvalidOperationException("No individuals found in population");
            _bestIndividuals.Add(bestIndividual);
        }
    }

    public void UpdateGenerationCounter() => CurrentGenerationIndex++;

    public void Reset()
    {
        lock (_listLock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));
            
            _averageFitness.Clear();
            _bestFitness.Clear();
            _worstFitness.Clear();
            _totalFitness.Clear();
            _numberOfUniqueIndividuals.Clear();
            _diversity.Clear();
            _bestIndividuals.Clear();
            _fitnessStandardDeviation.Clear();
            
            CurrentPopulation = null;
            CurrentGenerationIndex = 0;
        }
    }
    

    public void Dispose()
    {
      if (_isDisposed) return;
        
      lock (_listLock)
      {
        if (_isDisposed) return;
            
        var keys = _instances.Keys.ToList();
        foreach (var key in keys)
        {
          _instances.TryRemove(key, out _);
        }
            
        _isDisposed = true;
      }
        GC.SuppressFinalize(this);
    }

    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Individual.IndividualConverter() }
        };

        return JsonConvert.SerializeObject(this, settings);
    }

    public static EvolutionObserver? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new Individual.IndividualConverter() },
            ContractResolver = new PrivateSetterContractResolver()
        };

        return JsonConvert.DeserializeObject<EvolutionObserver>(json, settings);
    }
}