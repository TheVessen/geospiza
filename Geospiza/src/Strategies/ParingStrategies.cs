using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface IPairingStrategy
{
    List<Tuple<Individual, Individual>> PairIndividuals(List<Individual> selectedIndividuals, double inBreedingFactor);
}

public class InbreedingPairingStrategy : IPairingStrategy
{
    public List<Tuple<Individual, Individual>> PairIndividuals(List<Individual> selectedIndividuals, double inBreedingFactor)
    {
        var pairs = new List<Tuple<Individual, Individual>>();
        var random = new Random();

        foreach (var individual in selectedIndividuals)
        {
            Individual mate = FindMate(individual, selectedIndividuals, inBreedingFactor);
            pairs.Add(new Tuple<Individual, Individual>(individual, mate));
        }

        return pairs;
    }

    private Individual FindMate(Individual individual, List<Individual> potentialMates, double inBreedingFactor)
    {
        // Sort potential mates by genomic distance
        var sortedMates = potentialMates.OrderBy(mate => CalculateGenomicDistance(individual, mate)).ToList();

        // Select mate based on in-breeding factor
        int mateIndex = (int)((inBreedingFactor + 1) / 2 * (sortedMates.Count - 1));
        return sortedMates[mateIndex];
    }

    private double CalculateGenomicDistance(Individual ind1, Individual ind2)
    {
        // Example: Euclidean distance calculation
        double distance = 0;
        for (int i = 0; i < ind1._genePool.Count; i++)
        {
            distance += Math.Pow(ind1._genePool[i].TickValue - ind2._genePool[i].TickValue, 2);
        }
        return Math.Sqrt(distance);
    }
}