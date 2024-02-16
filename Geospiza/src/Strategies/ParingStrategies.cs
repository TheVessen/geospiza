using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies.Pairing;

public interface IPairingStrategy
{
    
    List<Tuple<Individual, Individual>> PairIndividuals(List<Individual> selectedIndividuals);
}

public class InbreedingPairingStrategy : IPairingStrategy
{
    
    private double InBreedingFactor { get; init; }
    
    public InbreedingPairingStrategy(double inBreedingFactor)
    {
        InBreedingFactor = inBreedingFactor;
    }
    
    public List<Tuple<Individual, Individual>> PairIndividuals(List<Individual> selectedIndividuals)
    {
        var pairs = new List<Tuple<Individual, Individual>>();
        foreach (var individual in selectedIndividuals)
        {
            Individual mate = FindMate(individual, selectedIndividuals, InBreedingFactor);
            pairs.Add(new Tuple<Individual, Individual>(individual, mate));
        }

        return pairs;
    }

    private Individual FindMate(Individual individual, List<Individual> potentialMates, double inBreedingFactor)
    {
        // Sort potential mates by genomic distance
        var sortedMates = potentialMates.OrderBy(mate => CalculateGenomicDistance(individual, mate)).ToList();

        // Select mate based on in-breeding factor
        var mateIndex = (int)((inBreedingFactor + 1) / 2 * (sortedMates.Count - 1));
        return sortedMates[mateIndex];
    }

    private double CalculateGenomicDistance(Individual ind1, Individual ind2)
    {
        // Example: Euclidean distance calculation
        double distance = 0;
        for (int i = 0; i < ind1.GenePool.Count; i++)
        {
            distance += Math.Pow(ind1.GenePool[i].TickValue - ind2.GenePool[i].TickValue, 2);
        }
        return Math.Sqrt(distance);
    }
}