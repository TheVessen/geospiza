using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface ICrossoverStrategy
{
    // public static List<Individual> Crosover(List<Individual> parents);
    public List<Individual> Crosover(List<Individual> parents);
}

public class SinglePointCrossover: ICrossoverStrategy
{
    private static Random _random = new Random();

    public  List<Individual> Crosover(List<Individual> parents)
    {
        // Assuming we're always dealing with pairs of parents
        // The list length should be even
        var offspring = new List<Individual>();

        for (int i = 0; i < parents.Count; i += 2)
        {
            var parent1 = parents[i];
            var parent2 = parents[i + 1];

            // Determine crossover point
            int crossoverPoint = _random.Next(1, parent1._genePool.Count);

            // Create offspring by combining genes from parents
            var child1Genes = parent1._genePool.Take(crossoverPoint)
                .Concat(parent2._genePool.Skip(crossoverPoint)).ToList();
            var child2Genes = parent2._genePool.Take(crossoverPoint)
                .Concat(parent1._genePool.Skip(crossoverPoint)).ToList();

            var child1 = new Individual();
            var child2 = new Individual();

            foreach (var gene in child1Genes)
            {
                child1.AddStableGene(new Gene(gene)); // Assuming StableGene can be copied this way
            }
            
            foreach (var gene in child2Genes)
            {
                child2.AddStableGene(new Gene(gene)); // Assuming StableGene can be copied this way
            }

            offspring.Add(child1);
            offspring.Add(child2);
        }

        return offspring;
    }
}