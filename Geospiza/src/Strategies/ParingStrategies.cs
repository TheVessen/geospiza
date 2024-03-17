using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies.Pairing;

public enum DistanceFunctionType
{
  Euclidean,
  Manhattan
}

public class PairingStrategy
{
  /// <summary>
  ///   Value 0 for Euchlidean distance and 1 for Manhattan distance
  /// </summary>
  public PairingStrategy(double inBreedingFactor,
    DistanceFunctionType distanceFunction = DistanceFunctionType.Manhattan)
  {
    InBreedingFactor = inBreedingFactor;
    DistanceFunction = distanceFunction;
  }

  /// <summary>
  ///   Gets the in-breeding factor used in the mate selection process.
  /// </summary>
  /// <remarks>
  ///   The in-breeding factor is a value between 0 and 1. It determines the preference
  ///   for selecting mates that are genetically similar (closer to 0) or dissimilar (closer to 1)
  ///   to the current individual. This factor is used in the FindMate method to select a mate from a sorted list of
  ///   potential mates.
  /// </remarks>
  public double InBreedingFactor { get; set; }

  private DistanceFunctionType DistanceFunction { get; }

  /// <summary>
  ///   Selects pairs of individuals to mate based on the in-breeding factor.
  /// </summary>
  /// <param name="selectedIndividuals"></param>
  /// <returns></returns>
  public List<Pair> PairIndividuals(List<Individual> selectedIndividuals)
  {
    var pairs = new List<Pair>();
    foreach (var individual in selectedIndividuals)
    {
      var mate = FindMate(individual, selectedIndividuals, InBreedingFactor);
      pairs.Add(new Pair(individual, mate));
    }

    return pairs;
  }

  /// <summary>
  ///   Finds a mate for the given individual from a list of potential mates.
  /// </summary>
  /// <param name="individual"></param>
  /// <param name="potentialMates"></param>
  /// <param name="inBreedingFactor"></param>
  /// <returns></returns>
  private Individual FindMate(Individual individual, IEnumerable<Individual> potentialMates, double inBreedingFactor)
  {
    List<Individual> sortedMates;

    // Sort potential mates by genomic distance

    if (DistanceFunction == DistanceFunctionType.Euclidean)
      sortedMates = potentialMates.OrderBy(mate => EuclideanDistance(individual, mate)).ToList();
    else
      sortedMates = potentialMates.OrderBy(mate => ManhattanDistance(individual, mate)).ToList();

    var mateIndex = (int)((inBreedingFactor + 1) / 2 * (sortedMates.Count - 1));
    return sortedMates[mateIndex];
  }

  /// <summary>
  ///   Calculates the genomic distance between two individuals.
  /// </summary>
  /// <param name="ind1">The first individual.</param>
  /// <param name="ind2">The second individual.</param>
  /// <returns>The genomic distance between the two individuals.</returns>
  /// <remarks>
  ///   This method uses the Euclidean distance calculation to determine the genomic distance.
  /// </remarks>
  private static double EuclideanDistance(Individual ind1, Individual ind2)
  {
    // Initialize distance to 0
    double distance = 0;

    // Iterate over the gene pool of the individuals
    for (var i = 0; i < ind1.GenePool.Count; i++)
      // Add the square of the difference between the tick values of the genes at the current index
      distance += Math.Pow(ind1.GenePool[i].TickValue - ind2.GenePool[i].TickValue, 2);

    // Return the square root of the total distance
    return Math.Sqrt(distance);
  }

  /// <summary>
  ///   Calculates the Manhattan distance between two individuals.
  /// </summary>
  /// <param name="ind1">The first individual.</param>
  /// <param name="ind2">The second individual.</param>
  /// <returns>The Manhattan distance between the two individuals.</returns>
  /// <remarks>
  ///   This method uses the Manhattan distance calculation to determine the genomic distance.
  /// </remarks>
  private static double ManhattanDistance(Individual ind1, Individual ind2)
  {
    // Initialize distance to 0
    double distance = 0;

    // Iterate over the gene pool of the individuals
    for (var i = 0; i < ind1.GenePool.Count; i++)
      // Add the absolute difference between the tick values of the genes at the current index
      distance += Math.Abs(ind1.GenePool[i].TickValue - ind2.GenePool[i].TickValue);

    // Return the total distance
    return distance;
  }
}