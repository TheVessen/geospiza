
using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies.Termination;

public static class TerminationStrategy
{
    public static double AssessFitnessImprovementRate(List<double> generationFitnessMap)
    {
        int windowSize = 5; // Last 5 generations
        if (generationFitnessMap.Count < windowSize + 1) 
        {
            return double.MaxValue; // Not enough data to assess
        }

        var recentGenerations = generationFitnessMap.TakeLast(windowSize + 1).ToList();
        double previousAverage = recentGenerations.Take(windowSize).Average();
        double currentAverage = recentGenerations.Skip(1).Average();

        return currentAverage - previousAverage; // Improvement in average fitness
    }
    
    public static double AssessGeneticDiversity(Population population)
    {
        double totalDistance = 0;
        int comparisons = 0;

        for (int i = 0; i < population.Count; i++)
        {
            for (int j = i + 1; j < population.Count; j++)
            {
                totalDistance += CalculateGenomicDistance(population.Inhabitants[i], population.Inhabitants[j]);
                comparisons++;
            }
        }

        return comparisons > 0 ? totalDistance / comparisons : 0;
    }

    private static double CalculateGenomicDistance(Individual ind1, Individual ind2)
    {
        double distance = 0;
        for (int i = 0; i < ind1.GenePool.Count; i++)
        {
            distance += Math.Pow(ind1.GenePool[i].TickValue - ind2.GenePool[i].TickValue, 2);
        }
        return Math.Sqrt(distance);
    }
    
    private static Individual _previousBestIndividual = null;

    public static double AssessBestIndividualProgress(Individual currentBestIndividual)
    {
        if (_previousBestIndividual == null)
        {
            _previousBestIndividual = currentBestIndividual;
            return double.MaxValue; // Initial generation
        }

        double progress = currentBestIndividual.Fitness - _previousBestIndividual.Fitness;
        _previousBestIndividual = currentBestIndividual;

        return progress;
    }
    
    public static double AssessFitnessImprovementRate(List<double> generationFitnessMap, int windowSize = 5)
    {
        if (generationFitnessMap.Count < windowSize + 1) 
        {
            return double.MaxValue; // Not enough data to assess
        }

        var recentGenerations = generationFitnessMap.TakeLast(windowSize + 1).ToList();
        double previousAverage = recentGenerations.Take(windowSize).Average();
        double currentAverage = recentGenerations.Skip(1).Average();

        return currentAverage - previousAverage; // Improvement in average fitness
    }
    
    public static List<double> NormalizeFitnessValues(List<double> fitnessValues)
    {
        double maxFitness = fitnessValues.Max();
        double minFitness = fitnessValues.Min();

        return fitnessValues.Select(fitness => (fitness - minFitness) / (maxFitness - minFitness)).ToList();
    }



}