using GeospizaCore.Core;

namespace GeospizaCore.Strategies;

public interface ISelectionStrategy
{
  List<Individual> Select(Population population, int numberOfSelections);
}

public abstract class SelectionStrategy : ISelectionStrategy
{
  protected readonly Random Random = new();
  public abstract List<Individual> Select(Population population, int numberOfSelections);
}

/// <summary>
///   Implements the Tournament Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///   Tournament Selection is a method used in genetic algorithms for selecting potentially useful solutions for
///   recombination.
///   In Tournament Selection, a subset of individuals is chosen from the population, and the individual with the highest
///   fitness in this group is selected.
///   The process is repeated until the desired number of individuals is selected.
///   This class requires the size of the tournament and the number of selections to be made as parameters.
///   The Select method randomly selects individuals for each tournament and chooses the best individual from each
///   tournament to be part of the next generation.
///   Note: This selection method maintains diversity in the population as it gives all individuals,
///   regardless of their fitness, a chance to be selected. However,
///   it also ensures that fitter individuals have a higher chance of being selected.
/// </remarks>
public class TournamentSelection : SelectionStrategy
{
  /// <summary>
  ///   The size of the tournament in tournament selection.
  /// </summary>
  private int TournamentSize { get; }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TournamentSelection" /> class.
  /// </summary>
  /// <param name="tournamentSize"></param>
  /// /// <remarks>
  ///   Tournament size is a crucial parameter in tournament selection strategy of an evolutionary algorithm.
  ///   It determines the number of individuals that are randomly selected from the population to compete in each tournament.
  ///   Key Impacts:
  ///   - Selection Pressure: A larger tournament size increases the selection pressure, making it more likely for higher
  ///   fitness individuals to be selected.
  ///   This can accelerate convergence towards optimal solutions but also raises the risk of premature convergence.
  ///   Conversely,
  ///   a smaller tournament size leads to lower selection pressure, allowing more diverse genetic material to be retained in
  ///   the population, thus promoting exploration of the solution space.
  ///   - Genetic Diversity: Smaller tournament sizes help maintain genetic diversity within the population, reducing the
  ///   likelihood of the
  ///   algorithm getting stuck in local optima. Larger tournament sizes can reduce diversity more rapidly as the fittest
  ///   individuals tend to dominate the selection process.
  ///   - Balancing Exploration and Exploitation: The tournament size plays a pivotal role in balancing exploration
  ///   (diversifying search to explore new areas of the solution space)
  ///   and exploitation (focusing on refining existing good solutions). Adjusting the tournament size can help achieve the
  ///   desired balance based on the specific problem and solution space characteristics.
  ///   It's essential to choose an appropriate tournament size based on the problem being solved. Experimentation and
  ///   parameter tuning may be required to find the optimal size for a specific application.
  /// </remarks>
  public TournamentSelection(int tournamentSize)
  {
    if (tournamentSize <= 0) throw new ArgumentException("Tournament size must be greater than 0");

    TournamentSize = tournamentSize;
  }

  public override List<Individual> Select(Population population, int numberOfSelections)
  {
    if (population == null) throw new ArgumentNullException(nameof(population));

    if (population.Inhabitants == null || !population.Inhabitants.Any())
      throw new ArgumentException("Population is empty");

    var selectedIndividuals = new List<Individual>();

    for (var i = 0; i < numberOfSelections; i++)
    {
      var tournament = new List<Individual>(TournamentSize);

      // Randomly select individuals for the tournament
      for (var j = 0; j < TournamentSize; j++)
      {
        var randomIndex = Random.Next(population.Inhabitants.Count);
        tournament.Add(population.Inhabitants[randomIndex]);
      }

      // Select the best individual from the tournament
      var bestIndividual = tournament.OrderBy(ind => ind.Fitness).First();
      selectedIndividuals.Add(bestIndividual);
    }

    return selectedIndividuals;
  }
}

/// <summary>
///   Implements the Roulette Wheel Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///   Roulette Wheel Selection, also known as fitness proportionate selection, is a method used
///   in genetic algorithms for selecting potentially useful solutions for recombination.
///   In Roulette Wheel Selection, the fitness of an individual is used to assign a probability of selection.
///   Think of it as a Roulette Wheel where each individual takes up a slice of the wheel, but the size of the slice is
///   proportional to the individual's fitness.
///   Those with the highest fitness have larger slices and therefore a higher chance of being selected.
///   This class requires the number of selections to be made as a parameter.
///   The Select method calculates the total fitness of the population and then iterates over the population, selecting
///   individuals based on their proportional fitness.
///   Note: This selection method can lead to premature convergence
///   as the fittest individuals are more likely to be selected, potentially reducing the genetic diversity in the
///   population.
/// </remarks>
public class RouletteWheelSelection : SelectionStrategy
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="RouletteWheelSelection" /> class.
  /// </summary>
  public RouletteWheelSelection()
  {
  }

  /// <summary>
  ///   Selects individuals from the population using the roulette wheel selection strategy.
  /// </summary>
  /// <param name="population"></param>
  /// <param name="numberOfSelections"></param>
  /// <returns></returns>
  /// <exception cref="System.InvalidOperationException"></exception>
  public override List<Individual> Select(Population population, int numberOfSelections)
  {
    var selectedIndividuals = new List<Individual>();
    var totalFitness = population.CalculateTotalFitness();

    // Handle case where total fitness is zero
    if (totalFitness == 0) throw new InvalidOperationException("Total fitness is zero, selection cannot be performed");

    for (var i = 0; i < numberOfSelections; i++)
    {
      var randomFitness = Random.NextDouble() * totalFitness;
      double runningSum = 0;

      foreach (var individual in population.Inhabitants)
      {
        runningSum += individual.Fitness;
        if (runningSum >= randomFitness)
        {
          selectedIndividuals.Add(individual);
          break;
        }
      }
    }

    return selectedIndividuals;
  }
}

/// <summary>
///   Implements the Pool Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///   Pool Selection is a method used in genetic algorithms for selecting potentially useful solutions for recombination.
///   In Pool Selection, each individual in the population is assigned a selection probability proportional to its fitness.
///   Then, a number of individuals are selected randomly based on these probabilities.
///   This process is repeated until the desired number of individuals is selected.
///   This class requires the number of selections to be made as a parameter.
///   The Select method calculates the selection probabilities for each individual in the population and then iterates over
///   the population,
///   selecting individuals based on these probabilities.
///   Note: This selection method maintains diversity in the population as it gives all individuals,
///   regardless of their fitness, a chance to be selected.
///   However, it also ensures that fitter individuals have a higher chance of being selected.
/// </remarks>
public class PoolSelection : SelectionStrategy
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="PoolSelection" /> class.
  /// </summary>
  public PoolSelection()
  {
  }

  /// <summary>
  ///   Selects individuals from the population using the pool selection strategy.
  /// </summary>
  /// <param name="population"></param>
  /// <param name="numberOfSelections"></param>
  /// <returns></returns>
  /// <exception cref="System.ArgumentException"></exception>
  public override List<Individual> Select(Population population, int numberOfSelections)
  {
    // Validate inputs
    if (population == null || !population.Inhabitants.Any()) throw new ArgumentException("Population is empty");

    if (numberOfSelections <= 0 || numberOfSelections > population.Inhabitants.Count)
      throw new ArgumentException("Invalid number of selections");

    population.CalculateProbability();

    var selectedIndividuals = new List<Individual>();
    for (var sel = 0; sel < numberOfSelections; sel++)
    {
      var r = Random.NextDouble();
      var i = 0;
      while (r > 0 && i < population.Inhabitants.Count)
      {
        r -= population.Inhabitants[i].Probability;
        i++;
      }

      selectedIndividuals.Add(population.Inhabitants[Math.Max(0, i - 1)]);
    }

    return selectedIndividuals;
  }
}

/// <summary>
///   Implements the Isotropic Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///   Isotropic Selection is a method used in genetic algorithms for selecting potentially useful solutions for
///   recombination.
///   In Isotropic Selection, each individual in the population has an equal chance of being selected, regardless of their
///   fitness.
///   This is similar to a roulette wheel selection where each individual occupies an equal slice of the wheel.
///   This class requires the number of selections to be made as a parameter.
///   The Select method randomly selects individuals from the population until the desired number of individuals is
///   selected.
///   Note: This selection method maintains maximum diversity in the population as it gives all individuals an equal chance
///   of being selected.
///   However, it does not favor fitter individuals, which can slow the algorithm's convergence to optimal solutions.
/// </remarks>
[Obsolete(
  "Due to bad performance, this class is marked as obsolete and will be removed in the future. Use one of the other selections instead.")]
public class IsotropicSelection : SelectionStrategy
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="IsotropicSelection" /> class.
  /// </summary>
  public IsotropicSelection()
  {
  }

  /// <summary>
  ///   Selects individuals from the population using the isotropic selection strategy.
  /// </summary>
  /// <param name="population"></param>
  /// <param name="numberOfSelections"></param>
  /// <returns></returns>
  public override List<Individual> Select(Population population, int numberOfSelections)
  {
    var selectedIndividuals = new List<Individual>();

    for (var i = 0; i < numberOfSelections; i++)
    {
      var randomIndex = Random.Next(population.Count);
      selectedIndividuals.Add(population.Inhabitants[randomIndex]);
    }

    return selectedIndividuals;
  }
}

/// <summary>
///   Implements the Exclusive Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///   Exclusive Selection is a method used in genetic algorithms for selecting potentially useful solutions for
///   recombination.
///   In Exclusive Selection, a certain percentage of the top-performing individuals in the population are selected.
///   This percentage is determined by the topPercentage parameter.
///   The individuals are selected based on their fitness, with higher fitness individuals being more likely to be
///   selected.
///   This class requires the top percentage of individuals to select and the number of selections to be made as
///   parameters.
///   The Select method sorts the population by fitness, calculates the cutoff index based on the top percentage, and then
///   selects individuals
///   from the sorted population until the desired number of individuals is selected or the cutoff index is reached.
///   Note: This selection method can lead to premature convergence as it heavily favors
///   the fittest individuals. However, it can also speed up the algorithm's convergence to optimal solutions.
/// </remarks>
[Obsolete(
  "Due to bad performance, this class is marked as obsolete and will be removed in the future. Use one of the other selections instead.")]
public class ExclusiveSelection : SelectionStrategy
{
  /// <summary>
  ///   Value between 0 and 1 representing the top percentage of individuals to selects
  /// </summary>
  private readonly double _topPercentage;

  /// <summary>
  ///   Initializes a new instance of the <see cref="ExclusiveSelection" /> class.
  /// </summary>
  /// <param name="topPercentage"></param>
  public ExclusiveSelection(double topPercentage)
  {
    if (topPercentage is <= 0 or > 1) throw new ArgumentException("Top percentage must be between 0 and 1");

    _topPercentage = topPercentage;
  }

  /// <summary>
  ///   Selects individuals from the population using the exclusive selection strategy.
  /// </summary>
  /// <param name="population"></param>
  /// <param name="numberOfSelections"></param>
  /// <returns></returns>
  public override List<Individual> Select(Population population, int numberOfSelections)
  {
    var selectedIndividuals = new List<Individual>();

    // Sort the population by fitness
    var sortedPopulation = population.Inhabitants.OrderBy(individual => individual.Fitness).ToList();

    // Select the top N%
    var cutoffIndex = (int)(population.Count * _topPercentage);
    for (var i = 0; i < numberOfSelections && i < cutoffIndex; i++) selectedIndividuals.Add(sortedPopulation[i]);

    return selectedIndividuals;
  }
}

/// <summary>
///   Implements the Stochastic Universal Sampling (SUS) strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///   Stochastic Universal Sampling is a method used in genetic algorithms for selecting potentially useful solutions for
///   recombination.
///   It is a type of fitness proportionate selection. The main advantage of SUS over simple roulette wheel selection is
///   that it ensures a spread of selection points, which promotes preservation of diversity.
///   In SUS, the fitness of each individual is used to assign a probability of selection. However, instead of selecting
///   individuals one at a time, SUS selects all individuals at once by spreading out evenly spaced pointers over the
///   population's
///   fitness values sorted in ascending order. This ensures a more even spread of selection points and helps maintain
///   diversity in the population.
///   This class requires the number of selections to be made as a parameter.
///   The Select method calculates the total fitness of the population, generates a random starting point, and then selects
///   individuals based on their proportional fitness.
///   The starting point is incremented by a fixed distance for each selection, ensuring a spread of selection points
///   across the population.
///   Note: This selection method maintains diversity in the population as it gives all individuals,
///   regardless of their fitness, a chance to be selected. However, it also ensures that fitter individuals have a higher
///   chance of being selected.
/// </remarks>
public class StochasticUniversalSampling : SelectionStrategy
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="StochasticUniversalSampling" /> class.
  /// </summary>
  public StochasticUniversalSampling()
  {
  }

  /// <summary>
  ///   Selects individuals from the population using the Stochastic Universal Sampling strategy.
  /// </summary>
  /// <param name="population">The population from which to select individuals.</param>
  /// <param name="numberOfSelections"></param>
  /// <returns>A list of selected individuals.</returns>
  public override List<Individual> Select(Population population, int numberOfSelections)
  {
    var selectedIndividuals = new List<Individual>();
    var totalFitness = population.Inhabitants.Sum(individual => individual.Fitness);

    var distance = 1.0 / numberOfSelections;
    var start = Random.NextDouble() * distance;

    for (var i = 0; i < numberOfSelections; i++)
    {
      var selectionPoint = start + i * distance;
      double runningSum = 0;

      foreach (var individual in population.Inhabitants)
      {
        runningSum += individual.Fitness / totalFitness;
        if (runningSum >= selectionPoint)
        {
          selectedIndividuals.Add(individual);
          break;
        }
      }
    }

    return selectedIndividuals;
  }
}