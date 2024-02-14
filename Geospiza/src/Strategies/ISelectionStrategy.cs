using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface ISelectionStrategy
{
    List<Individual> Select(List<Individual> population, int numberOfSelections);
}

/// <summary>
/// 
/// </summary>
public class TournamentSelection : ISelectionStrategy
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
    private readonly Random _random;

    public TournamentSelection(int tournamentSize)
    {
        _tournamentSize = tournamentSize;
        _random = new Random();
    }

    /// <summary>
    /// <see cref="https://www.baeldung.com/cs/ga-tournament-selection"/>
    /// </summary>
    /// <param name="population"></param>
    /// <param name="numberOfSelections"></param>
    /// <returns></returns>
    public List<Individual> Select(List<Individual> population, int numberOfSelections)
    {
        var selectedIndividuals = new List<Individual>();
        var totalFitness = population.Select(e => e.Fitness).Sum();

        for (var i = 0; i < numberOfSelections; i++)
        {
            // Use RouletteWheelSelection for half of the selections
            if (i < numberOfSelections / 2)
            {
                var randomFitness = _random.NextDouble() * totalFitness;
                double runningSum = 0;

                foreach (var individual in population)
                {
                    runningSum += individual.Fitness;
                    if (!(runningSum >= randomFitness)) continue;
                    selectedIndividuals.Add(individual);
                    break;
                }
            }
            // Use TournamentSelection for the other half
            else
            {
                var tournament = new List<Individual>();
                for (var j = 0; j < _tournamentSize; j++)
                {
                    var randomIndex = _random.Next(population.Count);
                    tournament.Add(population[randomIndex]);
                }

                // Select the best individual from the tournament
                var best = tournament.OrderByDescending(ind => ind.Fitness).First();
                selectedIndividuals.Add(best);
            }
        }

        return selectedIndividuals;
    }
}

/// <summary>
/// 
/// </summary>
public class RouletteWheelSelection : ISelectionStrategy
{
    private readonly Random _random = new();

    public List<Individual> Select(List<Individual> population, int numberOfSelections)
    {
        var selectedIndividuals = new List<Individual>();
        var totalFitness = CalculateTotalFitness(population);

        for (var i = 0; i < numberOfSelections; i++)
        {
            var randomFitness = _random.NextDouble() * totalFitness;
            double runningSum = 0;

            foreach (var individual in population)
            {
                runningSum += individual.Fitness;
                if (!(runningSum >= randomFitness)) continue;
                selectedIndividuals.Add(individual);
                break;
            }
        }

        return selectedIndividuals;
    }

    private static double CalculateTotalFitness(IEnumerable<Individual> population)
    {
        return population.Sum(individual => individual.Fitness);
    }
}