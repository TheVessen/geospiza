using System;
using System.Collections.Generic;
using Geospiza.Core;

namespace Geospiza.Strategies.Crossover;

public interface ICrossoverStrategy
{
    public double CrossoverRate { get; init; }
    public List<Individual> Crossover(Individual parent1, Individual parent2);
}

public abstract class CrossoverStrategy: ICrossoverStrategy
{
    public double CrossoverRate { get; init; }
    protected readonly Random Random = new Random();
    
    public abstract List<Individual> Crossover(Individual parent1, Individual parent2);
}

/// <summary>
/// Represents a single point crossover strategy.
/// </summary>
/// <remarks>
///Source: https://en.wikipedia.org/wiki/Crossover_(genetic_algorithm)
/// </remarks>
public class SinglePointCrossover: CrossoverStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SinglePointCrossover"/> class.
    /// </summary>
    /// <param name="crossoverRate">The crossover rate.</param>
    public SinglePointCrossover(double crossoverRate)
    {
        CrossoverRate = crossoverRate;
    }

    /// <summary>
    /// Performs a single point crossover between two parents.
    /// </summary>
    /// <param name="parent1">The first parent.</param>
    /// <param name="parent2">The second parent.</param>
    /// <returns>A list of offspring resulting from the crossover.</returns>
    /// <exception cref="ArgumentException">Thrown when the parents have genomes of different lengths.</exception>
    /// <remarks>
    ///Source: https://en.wikipedia.org/wiki/Crossover_(genetic_algorithm)
    /// </remarks>
    public override List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        var genePoolP1 = parent1.GenePool;
        var genePoolP2 = parent2.GenePool;

        if (genePoolP1.Count != genePoolP2.Count)
        {
            throw new ArgumentException("Parents must have genomes of the same length");
        }

        int genomeLength = genePoolP1.Count;
        int crossoverPoint = Random.Next(1, genomeLength); // Exclude first index to ensure actual crossover

        var child1Genome = new List<Gene>();
        var child2Genome = new List<Gene>();

        for (int i = 0; i < genomeLength; i++)
        {
            child1Genome.Add(i < crossoverPoint ? genePoolP1[i] : genePoolP2[i]);
            child2Genome.Add(i < crossoverPoint ? genePoolP2[i] : genePoolP1[i]);
        }

        Individual child1 = new Individual(child1Genome);
        Individual child2 = new Individual(child2Genome);

        return new List<Individual> { child1, child2 };
    }
}

/// <summary>
/// Represents a two point crossover strategy.
/// </summary>
public class TwoPointCrossover : CrossoverStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwoPointCrossover"/> class.
    /// </summary>
    /// <param name="crossoverRate">The crossover rate.</param>
    public TwoPointCrossover(double crossoverRate)
    {
        CrossoverRate = crossoverRate;
    }

    /// <summary>
    /// Performs a two point crossover between two parents.
    /// </summary>
    /// <param name="parent1">The first parent.</param>
    /// <param name="parent2">The second parent.</param>
    /// <returns>A list of offspring resulting from the crossover.</returns>
    /// <exception cref="ArgumentException">Thrown when the parents have genomes of different lengths.</exception>
    public override List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        // Ensure parents have the same number of genes
        if (parent1.GenePool.Count != parent2.GenePool.Count)
        {
            throw new ArgumentException("Parents must have genomes of the same length");
        }

        var genomeLength = parent1.GenePool.Count;

        // Choose two crossover points
        var crossoverPoint1 = Random.Next(0, genomeLength);
        var crossoverPoint2 = Random.Next(0, genomeLength);

        int counter = 0;
        // Ensure crossoverPoint1 is not equal to crossoverPoint2
        while (crossoverPoint1 == crossoverPoint2)
        {
            crossoverPoint2 = Random.Next(0, genomeLength);
            counter++;
            if (counter > 10)
            {
                break;
            }
        }
        
        counter = 0;

        // Ensure crossoverPoint1 is less than crossoverPoint2
        if (crossoverPoint1 > crossoverPoint2)
        {
            (crossoverPoint1, crossoverPoint2) = (crossoverPoint2, crossoverPoint1);
        }

        var child1Genome = new List<Gene>();
        var child2Genome = new List<Gene>();

        // Perform crossover
        for (var i = 0; i < genomeLength; i++)
        {
            if (i < crossoverPoint1 || i > crossoverPoint2)
            {
                child1Genome.Add(parent1.GenePool[i]);
                child2Genome.Add(parent2.GenePool[i]);
            }
            else
            {
                child1Genome.Add(parent2.GenePool[i]); child2Genome.Add(parent1.GenePool[i]);
            }
        }

        var child1 = new Individual(child1Genome);
        var child2 = new Individual(child2Genome);

        return new List<Individual> { child1, child2 };
    }
}
