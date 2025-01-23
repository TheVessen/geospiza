using System.Collections.Concurrent;
using GeospizaManager.Utils;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GeospizaManager.Core;

/// <summary>
/// Observes and tracks the evolution of a population across generations.
/// Implements the Singleton pattern per GH_Component and provides thread-safe access to evolution metrics.
/// </summary>
/// <remarks>
/// This class maintains statistics about population fitness, diversity, and best individuals
/// across generations. It supports JSON serialization for data persistence and transfer.
/// Use <see cref="ObserverServerSnapshot"/> for a simplified implementation
/// </remarks>
public class EvolutionObserver
{
  /// <summary>
  /// Thread-safe dictionary storing observer instances per Grasshopper component
  /// </summary>
  private static readonly ConcurrentDictionary<GH_Component, EvolutionObserver> _instances = new();

  /// <summary>
  /// Lock object for thread-safe access to internal lists
  /// </summary>
  private readonly object _listLock = new();

  /// <summary>
  /// Gets the current generation number being observed
  /// </summary>
  public int CurrentGenerationIndex { get; private set; }

  /// <summary>
  /// Gets the current population under observation
  /// </summary>
  public Population CurrentPopulation { get; private set; }

  /// <summary>
  /// Gets the average fitness of the population across generations
  /// </summary>
  public IReadOnlyList<double> AverageFitness => _averageFitness;

  /// <summary>
  /// Gets the best fitness of the population across generations
  /// </summary>
  public IReadOnlyList<double> BestFitness => _bestFitness;

  /// <summary>
  /// Gets the worst fitness of the population across generations
  /// </summary>
  public IReadOnlyList<double> WorstFitness => _worstFitness;

  /// <summary>
  /// Gets the total fitness of the population across generations
  /// </summary>
  public IReadOnlyList<double> TotalFitness => _totalFitness;

  /// <summary>
  /// Gets the number of unique individuals in the population across generations
  /// </summary>
  public IReadOnlyList<int> NumberOfUniqueIndividuals => _numberOfUniqueIndividuals;

  /// <summary>
  /// Gets the diversity of the population across generations
  /// </summary>
  public IReadOnlyList<int> Diversity => _diversity;

  /// <summary>
  /// Gets the best individuals of the population across generations
  /// </summary>
  public IReadOnlyList<Individual> BestIndividuals => _bestIndividuals;

  /// <summary>
  /// Gets the standard deviation of fitness values across generations
  /// </summary>
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

  private EvolutionObserver()
  {
  }

  /// <summary>
  /// Creates or retrieves an EvolutionObserver instance for a specific Grasshopper component
  /// </summary>
  /// <param name="solver">The Grasshopper component requiring observation</param>
  /// <returns>An EvolutionObserver instance</returns>
  /// <exception cref="ArgumentNullException">Thrown when solver is null</exception>
  public static EvolutionObserver GetInstance(GH_Component solver)
  {
    if (_instances.TryGetValue(solver, out var observer) && observer._isDisposed) _instances.TryRemove(solver, out _);
    return _instances.GetOrAdd(solver, _ => new EvolutionObserver());
  }

  /// <summary>
  /// Takes a snapshot of the current population's statistics
  /// </summary>
  /// <param name="currentPopulation">The population to analyze</param>
  public void Snapshot(Population currentPopulation)
  {

    lock (_listLock)
    {
      if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));

      var inhabitants = currentPopulation.Inhabitants;
      _bestFitness.Add(inhabitants.Max(inh => inh.Fitness));
      _worstFitness.Add(inhabitants.Min(inh => inh.Fitness));
      _averageFitness.Add(currentPopulation.GetAverageFitness());
      _totalFitness.Add(currentPopulation.CalculateTotalFitness());
      _numberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());

      var standardDeviation = CalculateStandardDeviation(inhabitants);
      _fitnessStandardDeviation.Add(standardDeviation);
    }

    SetPopulation(currentPopulation);
    UpdateGenerationCounter();
  }

  private static double CalculateStandardDeviation(IEnumerable<Individual> inhabitants)
  {
    var fitnessList = inhabitants.Select(i => i.Fitness).ToList();
    var average = fitnessList.Average();
    var sumOfSquaresOfDifferences = fitnessList.Sum(val => Math.Pow(val - average, 2));
    return Math.Sqrt(sumOfSquaresOfDifferences / fitnessList.Count);
  }

  /// <summary>
  /// Sets the current population and records its best individual
  /// </summary>
  private void SetPopulation(Population population)
  {

    lock (_listLock)
    {
      if (_isDisposed) throw new ObjectDisposedException(nameof(EvolutionObserver));

      CurrentPopulation = population;
      var bestIndividual = population.SelectTopIndividuals(1).FirstOrDefault()
                           ?? throw new InvalidOperationException("No individuals found in population");
      _bestIndividuals.Add(bestIndividual);
    }
  }

  /// <summary>
  /// Increments the generation counter by one
  /// </summary>
  private void UpdateGenerationCounter()
  {
    CurrentGenerationIndex++;
  }

/// <summary>
/// Resets the observer instance to its initial state
/// </summary>
/// <exception cref="InvalidOperationException"></exception>
  public void Reset()
  {
    lock (_listLock)
    {
      if (_isDisposed) throw new InvalidOperationException("Cannot reset a disposed EvolutionObserver instance.");

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

/// <summary>
/// Serializes the observer instance to a JSON string
/// </summary>
/// <returns></returns>
  public string ToJson()
  {
    var settings = new JsonSerializerSettings
    {
      Formatting = Formatting.Indented,
      Converters = new List<JsonConverter> { new Individual.IndividualConverter() }
    };

    return JsonConvert.SerializeObject(this, settings);
  }

/// <summary>
/// Tries to deserialize a JSON string into an EvolutionObserver instance
/// </summary>
/// <param name="json"></param>
/// <returns></returns>
  public static EvolutionObserver? FromJson(string json)
  {

    var settings = new JsonSerializerSettings
    {
      Converters = new List<JsonConverter> { new Individual.IndividualConverter() },
      ContractResolver = new PrivateSetterContractResolver()
    };

    return JsonConvert.DeserializeObject<EvolutionObserver>(json, settings);
  }
}