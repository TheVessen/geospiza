using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface ISelectionStrategy
{
    List<Individual> Select(Population population);
}

public abstract class SelectionStrategy : ISelectionStrategy
{
    public abstract List<Individual> Select(Population population);
    protected readonly Random Random = new Random();
}

public class TournamentSelection : SelectionStrategy
{
    /// <summary>
    /// The size of the tournament in tournament selection.
    /// </summary>
    /// <remarks>
    /// Tournament size is a crucial parameter in tournament selection strategy of an evolutionary algorithm.
    /// It determines the number of individuals that are randomly selected from the population to compete in each tournament.
    /// 
    /// Key Impacts:
    /// - Selection Pressure: A larger tournament size increases the selection pressure, making it more likely for higher fitness individuals to be selected. This can accelerate convergence towards optimal solutions but also raises the risk of premature convergence. Conversely, a smaller tournament size leads to lower selection pressure, allowing more diverse genetic material to be retained in the population, thus promoting exploration of the solution space.
    /// - Genetic Diversity: Smaller tournament sizes help maintain genetic diversity within the population, reducing the likelihood of the algorithm getting stuck in local optima. Larger tournament sizes can reduce diversity more rapidly as the fittest individuals tend to dominate the selection process.
    /// - Balancing Exploration and Exploitation: The tournament size plays a pivotal role in balancing exploration (diversifying search to explore new areas of the solution space) and exploitation (focusing on refining existing good solutions). Adjusting the tournament size can help achieve the desired balance based on the specific problem and solution space characteristics.
    ///
    /// It's essential to choose an appropriate tournament size based on the problem being solved. Experimentation and parameter tuning may be required to find the optimal size for a specific application.
    /// </remarks>
    private readonly int _tournamentSize;
    private readonly int _numberOfSelections;

    public TournamentSelection(int tournamentSize, int numberOfSelections)
    {
        if (tournamentSize <= 0)
        {
            throw new ArgumentException("Tournament size must be greater than 0");
        }

        if (numberOfSelections < 1)
        {
            throw new ArgumentException("Number of selections must be greater than 0");
        }

        _tournamentSize = tournamentSize;
        _numberOfSelections = numberOfSelections;
    }
    
    public TournamentSelection()
    {
        _tournamentSize = 5;
        _numberOfSelections = 2;
    }

    public override List<Individual> Select(Population population)
    {
        if (population == null)
        {
            throw new ArgumentNullException(nameof(population));
        }

        if (population.Inhabitants == null || !population.Inhabitants.Any())
        {
            throw new ArgumentException("Population is empty");
        }

        var selectedIndividuals = new List<Individual>();

        for (int i = 0; i < _numberOfSelections; i++)
        {
            var tournament = new List<Individual>(_tournamentSize);

            // Randomly select individuals for the tournament
            for (int j = 0; j < _tournamentSize; j++)
            {
                int randomIndex = Random.Next(population.Inhabitants.Count);
                tournament.Add(population.Inhabitants[randomIndex]);
            }

            // Select the best individual from the tournament
            var bestIndividual = tournament.MaxBy(ind => ind.Fitness);
            selectedIndividuals.Add(bestIndividual);
        }

        return selectedIndividuals;
    }
}

/// <summary>
/// 
/// </summary>
public class RouletteWheelSelection : SelectionStrategy
{
    private readonly int _numberOfSelections;
    RouletteWheelSelection(int numberOfSelections)
    {
        if (numberOfSelections <= 0)
        {
            throw new ArgumentException("Number of selections must be greater than 0");
        }

        this._numberOfSelections = numberOfSelections;
    }
    
    public override List<Individual> Select(Population population)
    {
        var selectedIndividuals = new List<Individual>();
        var totalFitness = population.CalculateTotalFitness();

        // Handle case where total fitness is zero
        if (totalFitness == 0)
        {
            throw new InvalidOperationException("Total fitness is zero, selection cannot be performed");
        }

        for (var i = 0; i < _numberOfSelections; i++)
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

public class PoolSelection : SelectionStrategy
{
    private readonly int _numberOfSelections;
    
    public PoolSelection(int numberOfSelections)
    {
        if (numberOfSelections <= 0)
        {
            throw new ArgumentException("Number of selections must be greater than 0");
        }

        _numberOfSelections = numberOfSelections;
    }
    
    public override List<Individual> Select(Population population)
    {
        // Validate inputs
        if (population == null || !population.Inhabitants.Any())
        {
            throw new ArgumentException("Population is empty");
        }

        if (_numberOfSelections <= 0 || _numberOfSelections > population.Inhabitants.Count)
        {
            throw new ArgumentException("Invalid number of selections");
        }

        population.CalculateProbability();

        var selectedIndividuals = new List<Individual>();
        for (int sel = 0; sel < _numberOfSelections; sel++)
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


public class IsotropicSelection : SelectionStrategy
{
    private readonly int _numberOfSelections;
    
    public IsotropicSelection(int numberOfSelections)
    {
        if (numberOfSelections <= 0)
        {
            throw new ArgumentException("Number of selections must be greater than 0");
        }

        this._numberOfSelections = numberOfSelections;
    }
    
    public override List<Individual> Select(Population population)
    {
        var selectedIndividuals = new List<Individual>();

        for (int i = 0; i < _numberOfSelections; i++)
        {
            int randomIndex = Random.Next(population.Count);
            selectedIndividuals.Add(population.Inhabitants[randomIndex]);
        }

        return selectedIndividuals;
    }
}

public class ExclusiveSelection : SelectionStrategy
{
    /// <summary>
    /// Value between 0 and 1 representing the top percentage of individuals to selects
    /// </summary>
    private readonly double _topPercentage;
    private readonly int _numberOfSelections;

    public ExclusiveSelection(double topPercentage, int numberOfSelections)
    {
        if (topPercentage is <= 0 or > 1)
        {
            throw new ArgumentException("Top percentage must be between 0 and 1");
        }

        _topPercentage = topPercentage;
        _numberOfSelections = numberOfSelections;
    }

    public override List<Individual> Select(Population population)
    {
        var selectedIndividuals = new List<Individual>();

        // Sort the population by fitness
        var sortedPopulation = population.Inhabitants.OrderBy(individual => individual.Fitness).ToList();

        // Select the top N%
        var cutoffIndex = (int)(population.Count * _topPercentage);
        for (var i = 0; i < _numberOfSelections && i < cutoffIndex; i++)
        {
            selectedIndividuals.Add(sortedPopulation[i]);
        }

        return selectedIndividuals;
    }
}

public class BiasedSelection : SelectionStrategy
{
    private readonly int _numberOfSelections;
    
    public BiasedSelection(int numberOfSelections)
    {
        if (numberOfSelections <= 0)
        {
            throw new ArgumentException("Number of selections must be greater than 0");
        }

        this._numberOfSelections = numberOfSelections;
    }
    
    
    public override List<Individual> Select(Population population)
    {
        var selectedIndividuals = new List<Individual>();
        var totalFitness = population.Inhabitants.Sum(individual => individual.Fitness);

        for (int i = 0; i < _numberOfSelections; i++)
        {
            double selectionPoint = Random.NextDouble() * totalFitness;
            double runningSum = 0;

            foreach (var individual in population.Inhabitants)
            {
                runningSum += individual.Fitness;
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
