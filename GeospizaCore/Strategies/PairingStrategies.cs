using System.Collections.Generic;
using GeospizaCore.Core;

namespace GeospizaCore.Strategies;

public enum DistanceFunctionType
{
    Euclidean,
    Manhattan
}

public class PairingStrategy
{
    // Cached once at construction to avoid a delegate allocation per FindMate call.
    private readonly Func<Individual, Individual, double> _distanceFn;

    /// <summary>
    ///     Creates a new pairing strategy with the given in-breeding factor and distance function.
    /// </summary>
    public PairingStrategy(double inBreedingFactor,
        DistanceFunctionType distanceFunction = DistanceFunctionType.Manhattan)
    {
        InBreedingFactor = inBreedingFactor;
        DistanceFunction = distanceFunction;
        _distanceFn = distanceFunction == DistanceFunctionType.Euclidean ? EuclideanDistance : ManhattanDistance;
    }

    /// <summary>
    ///     Controls mate similarity preference (−1 to 1): negative favours similar mates, positive favours dissimilar ones.
    /// </summary>
    public double InBreedingFactor { get; set; }

    public DistanceFunctionType DistanceFunction { get; }

    /// <summary>
    ///     Lazily yields one mating pair per individual in the pool.
    ///     Lazy evaluation means <see cref="FindMate" /> is only called for pairs that are actually consumed,
    ///     which avoids redundant distance calculations when the caller breaks early.
    /// </summary>
    public IEnumerable<IndividualPair> PairIndividuals(List<Individual> selectedIndividuals)
    {
        foreach (var individual in selectedIndividuals)
            yield return new IndividualPair(individual, FindMate(individual, selectedIndividuals));
    }

    /// <summary>
    ///     Sorts the pool by genomic distance from <paramref name="individual" /> and returns the mate
    ///     at the index mapped by <see cref="InBreedingFactor" />.
    /// </summary>
    private Individual FindMate(Individual individual, List<Individual> potentialMates)
    {
        var sortedMates = potentialMates
            .Where(mate => !ReferenceEquals(mate, individual))
            .OrderBy(mate => _distanceFn(individual, mate))
            .ToList();

        if (sortedMates.Count == 0)
            return individual;

        var mateIndex = (int)((InBreedingFactor + 1) / 2 * (sortedMates.Count - 1));
        return sortedMates[mateIndex];
    }

    private static double EuclideanDistance(Individual ind1, Individual ind2)
    {
        double distance = 0;
        for (var i = 0; i < ind1.GenePool.Count; i++)
        {
            var delta = ind1.GenePool[i].TickValue - ind2.GenePool[i].TickValue;
            distance += delta * delta;
        }

        return Math.Sqrt(distance);
    }

    private static double ManhattanDistance(Individual ind1, Individual ind2)
    {
        double distance = 0;
        for (var i = 0; i < ind1.GenePool.Count; i++)
            distance += Math.Abs(ind1.GenePool[i].TickValue - ind2.GenePool[i].TickValue);

        return distance;
    }
}
