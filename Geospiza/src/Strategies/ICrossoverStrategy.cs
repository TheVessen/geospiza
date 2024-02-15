using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface ICrossoverStrategy
{
    // public static List<Individual> Crosover(List<Individual> parents);
    public List<Individual> Crossover(Individual parent1, Individual parent2);
}

public class SinglePointCrossover: ICrossoverStrategy
{
    private static Random _random = new Random();

    public List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        var genePoolP1 = parent1._genePool;
        var genePoolP2 = parent2._genePool;

        if (genePoolP1.Count != genePoolP2.Count)
        {
            throw new ArgumentException("Parents must have genomes of the same length");
        }

        int genomeLength = genePoolP1.Count;
        int crossoverPoint = _random.Next(1, genomeLength); // Exclude first index to ensure actual crossover

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

public class TwoPointCrossover : ICrossoverStrategy
{
    private static Random _random = new Random();

    public List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        // Ensure parents have the same number of genes
        if (parent1._genePool.Count != parent2._genePool.Count)
        {
            throw new ArgumentException("Parents must have genomes of the same length");
        }

        int genomeLength = parent1._genePool.Count;

        // Choose two crossover points
        int crossoverPoint1 = _random.Next(0, genomeLength);
        int crossoverPoint2 = _random.Next(0, genomeLength);

        // Ensure crossoverPoint1 is less than crossoverPoint2
        if (crossoverPoint1 > crossoverPoint2)
        {
            int temp = crossoverPoint1;
            crossoverPoint1 = crossoverPoint2;
            crossoverPoint2 = temp;
        }

        var child1Genome = new List<Gene>();
        var child2Genome = new List<Gene>();

        // Perform crossover
        for (int i = 0; i < genomeLength; i++)
        {
            if (i < crossoverPoint1 || i > crossoverPoint2)
            {
                child1Genome.Add(parent1._genePool[i]);
                child2Genome.Add(parent2._genePool[i]);
            }
            else
            {
                child1Genome.Add(parent2._genePool[i]);
                child2Genome.Add(parent1._genePool[i]);
            }
        }

        Individual child1 = new Individual(child1Genome);
        Individual child2 = new Individual(child2Genome);

        return new List<Individual> { child1, child2 };
    }
}
