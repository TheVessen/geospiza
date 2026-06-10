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
///     Tournament selection: competes a randomly selected subset of individuals and picks the fittest.
///     Provides a balance between selection pressure and diversity.
/// </summary>
/// <remarks>
///     Reference: J. Miller and W. M. Spears, "Automated assembly design synthesis with
///     a genetic algorithm," in Proceedings of the Evolutionary Programming Conference,
///     pp. 87–98, 1993.
///     Also: D. E. Goldberg, "Genetic Algorithms in Search, Optimization, and Machine Learning,"
///     Addison-Wesley, 1989.
/// </remarks>
public class TournamentSelection : SelectionStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TournamentSelection" /> class.
    /// </summary>
    /// <param name="tournamentSize"></param>
    /// ///
    /// <remarks>
    ///     Tournament size is a crucial parameter in tournament selection strategy of an evolutionary algorithm.
    ///     It determines the number of individuals that are randomly selected from the population to compete in each
    ///     tournament.
    ///     Key Impacts:
    ///     - Selection Pressure: A larger tournament size increases the selection pressure, making it more likely for higher
    ///     fitness individuals to be selected.
    ///     This can accelerate convergence towards optimal solutions but also raises the risk of premature convergence.
    ///     Conversely,
    ///     a smaller tournament size leads to lower selection pressure, allowing more diverse genetic material to be retained
    ///     in
    ///     the population, thus promoting exploration of the solution space.
    ///     - Genetic Diversity: Smaller tournament sizes help maintain genetic diversity within the population, reducing the
    ///     likelihood of the
    ///     algorithm getting stuck in local optima. Larger tournament sizes can reduce diversity more rapidly as the fittest
    ///     individuals tend to dominate the selection process.
    ///     - Balancing Exploration and Exploitation: The tournament size plays a pivotal role in balancing exploration
    ///     (diversifying search to explore new areas of the solution space)
    ///     and exploitation (focusing on refining existing good solutions). Adjusting the tournament size can help achieve the
    ///     desired balance based on the specific problem and solution space characteristics.
    ///     It's essential to choose an appropriate tournament size based on the problem being solved. Experimentation and
    ///     parameter tuning may be required to find the optimal size for a specific application.
    /// </remarks>
    public TournamentSelection(int tournamentSize)
    {
        if (tournamentSize <= 0) throw new ArgumentException("Tournament size must be greater than 0");

        TournamentSize = tournamentSize;
    }

    /// <summary>
    ///     The size of the tournament in tournament selection.
    /// </summary>
    public int TournamentSize { get; }

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

            // Select the best individual from the tournament (O(n) scan, no allocation)
            var bestIndividual = tournament[0];
            for (var j = 1; j < tournament.Count; j++)
                if (tournament[j].Fitness > bestIndividual.Fitness)
                    bestIndividual = tournament[j];
            selectedIndividuals.Add(bestIndividual);
        }

        return selectedIndividuals;
    }
}

/// <summary>
///     Roulette Wheel Selection (fitness-proportionate selection): each individual is selected
///     with probability proportional to its fitness relative to the total population fitness.
/// </summary>
/// <remarks>
///     Reference: D. E. Goldberg, "Genetic Algorithms in Search, Optimization, and
///     Machine Learning," Addison-Wesley, 1989.
///     Also known as Fitness-Proportionate Selection or Stochastic Acceptance.
/// </remarks>
public class RouletteWheelSelection : SelectionStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RouletteWheelSelection" /> class.
    /// </summary>
    public RouletteWheelSelection()
    {
    }

    /// <summary>
    ///     Selects individuals from the population using the roulette wheel selection strategy.
    /// </summary>
    /// <param name="population"></param>
    /// <param name="numberOfSelections"></param>
    /// <returns></returns>
    /// <exception cref="System.InvalidOperationException"></exception>
    public override List<Individual> Select(Population population, int numberOfSelections)
    {
        var selectedIndividuals = new List<Individual>();
        var inhabitants = population.Inhabitants;

        // Shift fitness values so the minimum becomes positive
        var minFitness = inhabitants[0].Fitness;
        for (var i = 1; i < inhabitants.Count; i++)
            if (inhabitants[i].Fitness < minFitness)
                minFitness = inhabitants[i].Fitness;

        // Offset ensures all values are > 0 (small epsilon avoids zero probability)
        var offset = minFitness < 0 ? -minFitness + 1.0 : 0.0;

        var totalFitness = 0.0;
        for (var i = 0; i < inhabitants.Count; i++)
            totalFitness += inhabitants[i].Fitness + offset;

        if (totalFitness == 0)
            throw new InvalidOperationException("Total fitness is zero, selection cannot be performed");

        for (var i = 0; i < numberOfSelections; i++)
        {
            var randomFitness = Random.NextDouble() * totalFitness;
            double runningSum = 0;
            var selected = inhabitants[inhabitants.Count - 1];

            foreach (var individual in inhabitants)
            {
                runningSum += individual.Fitness + offset;
                if (runningSum >= randomFitness)
                {
                    selected = individual;
                    break;
                }
            }

            selectedIndividuals.Add(selected);
        }

        return selectedIndividuals;
    }
}

/// <summary>
///     Implements the Pool Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///     Pool Selection is a method used in genetic algorithms for selecting potentially useful solutions for recombination.
///     In Pool Selection, each individual in the population is assigned a selection probability proportional to its
///     fitness.
///     Then, a number of individuals are selected randomly based on these probabilities.
///     This process is repeated until the desired number of individuals is selected.
///     This class requires the number of selections to be made as a parameter.
///     The Select method calculates the selection probabilities for each individual in the population and then iterates
///     over
///     the population,
///     selecting individuals based on these probabilities.
///     Note: This selection method maintains diversity in the population as it gives all individuals,
///     regardless of their fitness, a chance to be selected.
///     However, it also ensures that fitter individuals have a higher chance of being selected.
/// </remarks>
[Obsolete("Functionally identical to RouletteWheelSelection. Use RouletteWheelSelection instead.")]
public class PoolSelection : SelectionStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PoolSelection" /> class.
    /// </summary>
    public PoolSelection()
    {
    }

    /// <summary>
    ///     Selects individuals from the population using the pool selection strategy.
    /// </summary>
    /// <param name="population"></param>
    /// <param name="numberOfSelections"></param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentException"></exception>
    public override List<Individual> Select(Population population, int numberOfSelections)
    {
        // Validate inputs
        if (population == null || !population.Inhabitants.Any()) throw new ArgumentException("Population is empty");

        if (numberOfSelections <= 0)
            throw new ArgumentException("Number of selections must be greater than 0");

        var inhabitants = population.Inhabitants;

        // Shift fitness values so the minimum becomes positive
        var minFitness = inhabitants[0].Fitness;
        for (var i = 1; i < inhabitants.Count; i++)
            if (inhabitants[i].Fitness < minFitness)
                minFitness = inhabitants[i].Fitness;

        var offset = minFitness < 0 ? -minFitness + 1.0 : 0.0;

        var totalFitness = 0.0;
        for (var i = 0; i < inhabitants.Count; i++)
            totalFitness += inhabitants[i].Fitness + offset;

        if (totalFitness == 0)
            throw new InvalidOperationException("Total fitness is zero, selection cannot be performed");

        // Compute shifted probabilities
        var probabilities = new double[inhabitants.Count];
        for (var i = 0; i < inhabitants.Count; i++)
            probabilities[i] = (inhabitants[i].Fitness + offset) / totalFitness;

        var selectedIndividuals = new List<Individual>();
        for (var sel = 0; sel < numberOfSelections; sel++)
        {
            var r = Random.NextDouble();
            var cumulative = 0.0;
            var selectedIndex = inhabitants.Count - 1;

            for (var i = 0; i < inhabitants.Count; i++)
            {
                cumulative += probabilities[i];
                if (cumulative >= r)
                {
                    selectedIndex = i;
                    break;
                }
            }

            selectedIndividuals.Add(inhabitants[selectedIndex]);
        }

        return selectedIndividuals;
    }
}

/// <summary>
///     Implements the Isotropic Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///     Isotropic Selection is a method used in genetic algorithms for selecting potentially useful solutions for
///     recombination.
///     In Isotropic Selection, each individual in the population has an equal chance of being selected, regardless of
///     their
///     fitness.
///     This is similar to a roulette wheel selection where each individual occupies an equal slice of the wheel.
///     This class requires the number of selections to be made as a parameter.
///     The Select method randomly selects individuals from the population until the desired number of individuals is
///     selected.
///     Note: This selection method maintains maximum diversity in the population as it gives all individuals an equal
///     chance
///     of being selected.
///     However, it does not favor fitter individuals, which can slow the algorithm's convergence to optimal solutions.
/// </remarks>
[Obsolete(
    "Due to bad performance, this class is marked as obsolete and will be removed in the future. Use one of the other selections instead.")]
public class IsotropicSelection : SelectionStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="IsotropicSelection" /> class.
    /// </summary>
    public IsotropicSelection()
    {
    }

    /// <summary>
    ///     Selects individuals from the population using the isotropic selection strategy.
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
///     Implements the Exclusive Selection strategy for selecting individuals in a genetic algorithm.
/// </summary>
/// <remarks>
///     Exclusive Selection is a method used in genetic algorithms for selecting potentially useful solutions for
///     recombination.
///     In Exclusive Selection, a certain percentage of the top-performing individuals in the population are selected.
///     This percentage is determined by the topPercentage parameter.
///     The individuals are selected based on their fitness, with higher fitness individuals being more likely to be
///     selected.
///     This class requires the top percentage of individuals to select and the number of selections to be made as
///     parameters.
///     The Select method sorts the population by fitness, calculates the cutoff index based on the top percentage, and
///     then
///     selects individuals
///     from the sorted population until the desired number of individuals is selected or the cutoff index is reached.
///     Note: This selection method can lead to premature convergence as it heavily favors
///     the fittest individuals. However, it can also speed up the algorithm's convergence to optimal solutions.
/// </remarks>
[Obsolete(
    "Due to bad performance, this class is marked as obsolete and will be removed in the future. Use one of the other selections instead.")]
public class ExclusiveSelection : SelectionStrategy
{
    /// <summary>
    ///     Value between 0 and 1 representing the top percentage of individuals to selects
    /// </summary>
    private readonly double _topPercentage;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExclusiveSelection" /> class.
    /// </summary>
    /// <param name="topPercentage"></param>
    public ExclusiveSelection(double topPercentage)
    {
        if (topPercentage is <= 0 or > 1) throw new ArgumentException("Top percentage must be between 0 and 1");

        _topPercentage = topPercentage;
    }

    /// <summary>
    ///     Selects individuals from the population using the exclusive selection strategy.
    /// </summary>
    /// <param name="population"></param>
    /// <param name="numberOfSelections"></param>
    /// <returns></returns>
    public override List<Individual> Select(Population population, int numberOfSelections)
    {
        var selectedIndividuals = new List<Individual>();

        // Sort the population by fitness
        var sortedPopulation = population.Inhabitants.OrderByDescending(individual => individual.Fitness).ToList();

        // Select the top N%
        var cutoffIndex = Math.Max(1, (int)(population.Count * _topPercentage));
        for (var i = 0; i < numberOfSelections && i < cutoffIndex; i++) selectedIndividuals.Add(sortedPopulation[i]);

        return selectedIndividuals;
    }
}

/// <summary>
///     Stochastic Universal Sampling (SUS): distributes selection pointers evenly across the
///     fitness landscape to ensure spread-out selection while maintaining fitness-proportionality.
///     Reduces genetic drift and maintains diversity better than roulette wheel.
/// </summary>
/// <remarks>
///     Reference: J. E. Baker, "Reducing bias and inefficiency in the selection algorithm,"
///     in Proceedings of the Second International Conference on Genetic Algorithms and their
///     Application, pp. 14–21, July 1987.
/// </remarks>
public class StochasticUniversalSampling : SelectionStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StochasticUniversalSampling" /> class.
    /// </summary>
    public StochasticUniversalSampling()
    {
    }

    /// <summary>
    ///     Selects individuals from the population using the Stochastic Universal Sampling strategy.
    /// </summary>
    /// <param name="population">The population from which to select individuals.</param>
    /// <param name="numberOfSelections"></param>
    /// <returns>A list of selected individuals.</returns>
    public override List<Individual> Select(Population population, int numberOfSelections)
    {
        var selectedIndividuals = new List<Individual>();
        var inhabitants = population.Inhabitants;

        // Shift fitness values so the minimum becomes positive
        var minFitness = inhabitants[0].Fitness;
        for (var i = 1; i < inhabitants.Count; i++)
            if (inhabitants[i].Fitness < minFitness)
                minFitness = inhabitants[i].Fitness;

        var offset = minFitness < 0 ? -minFitness + 1.0 : 0.0;

        var totalFitness = 0.0;
        for (var i = 0; i < inhabitants.Count; i++)
            totalFitness += inhabitants[i].Fitness + offset;

        if (totalFitness == 0)
            throw new InvalidOperationException("Total fitness is zero, selection cannot be performed");

        var distance = 1.0 / numberOfSelections;
        var start = Random.NextDouble() * distance;

        for (var i = 0; i < numberOfSelections; i++)
        {
            var selectionPoint = start + i * distance;
            double runningSum = 0;
            var selected = inhabitants[inhabitants.Count - 1];

            foreach (var individual in inhabitants)
            {
                runningSum += (individual.Fitness + offset) / totalFitness;
                if (runningSum >= selectionPoint)
                {
                    selected = individual;
                    break;
                }
            }

            selectedIndividuals.Add(selected);
        }

        return selectedIndividuals;
    }
}