using GeospizaCore.Core;

namespace GeospizaCore.Strategies;

public enum DistanceFunctionType
{
    Euclidean,
    Manhattan
}

public interface IPairingStrategy
{
    IEnumerable<IndividualPair> PairIndividuals(List<Individual> selectedIndividuals);
}

/// <summary>Marker interface — strategy is only valid for single-objective solvers.</summary>
public interface ISingleObjectiveStrategy { }

/// <summary>Marker interface — strategy is only valid for multi-objective solvers (NSGA-II/III).</summary>
public interface IMultiObjectiveStrategy { }

public class PairingStrategy : IPairingStrategy
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

/// <summary>
///     Reference-point-based pairing for NSGA-III: pairs individuals within the same
///     reference-point niche to promote exploitation within niches while preserving
///     front diversity. Falls back to any partner when no niche-mate is available.
///     Reference: K. Deb and H. Jain, "An evolutionary many-objective optimization
///     algorithm using reference-point-based nondominated sorting approach, Part I:
///     Solving problems with box constraints," IEEE Transactions on Evolutionary
///     Computation, vol. 18, no. 4, pp. 577–601, Aug. 2014,
///     doi: 10.1109/TEVC.2013.2281534.
/// </summary>
public class ReferencePointPairingStrategy : IPairingStrategy, IMultiObjectiveStrategy
{
    private readonly Random _random = new();

    public IEnumerable<IndividualPair> PairIndividuals(List<Individual> selectedIndividuals)
    {
        foreach (var individual in selectedIndividuals)
            yield return new IndividualPair(individual, FindMate(individual, selectedIndividuals));
    }

    private Individual FindMate(Individual individual, List<Individual> pool)
    {
        var sameNiche = new List<Individual>();
        var others = new List<Individual>();
        foreach (var m in pool)
        {
            if (ReferenceEquals(m, individual)) continue;
            if (m.ReferencePointIndex == individual.ReferencePointIndex)
                sameNiche.Add(m);
            else
                others.Add(m);
        }

        var candidates = sameNiche.Count > 0 ? sameNiche : others;
        if (candidates.Count == 0) return individual;
        return candidates[_random.Next(candidates.Count)];
    }
}

/// <summary>
///     Rank-aware pairing for NSGA-II: pairs individuals within the same Pareto front
///     (rank) to keep crossover within quality-equivalent solutions and avoid rank dilution.
///     Falls back to any partner when no same-rank mate is available.
///     Reference: K. Deb, A. Pratap, S. Agarwal, and T. Meyarivan, "A fast and elitist
///     multiobjective genetic algorithm: NSGA-II," IEEE Transactions on Evolutionary
///     Computation, vol. 6, no. 2, pp. 182–197, Apr. 2002,
///     doi: 10.1109/4235.996017.
/// </summary>
public class RankAwarePairingStrategy : IPairingStrategy, IMultiObjectiveStrategy
{
    private readonly Random _random = new();

    public IEnumerable<IndividualPair> PairIndividuals(List<Individual> selectedIndividuals)
    {
        foreach (var individual in selectedIndividuals)
            yield return new IndividualPair(individual, FindMate(individual, selectedIndividuals));
    }

    private Individual FindMate(Individual individual, List<Individual> pool)
    {
        var sameRank = new List<Individual>();
        var others = new List<Individual>();
        foreach (var m in pool)
        {
            if (ReferenceEquals(m, individual)) continue;
            if (m.ParetoRank == individual.ParetoRank)
                sameRank.Add(m);
            else
                others.Add(m);
        }

        var candidates = sameRank.Count > 0 ? sameRank : others;
        if (candidates.Count == 0) return individual;
        return candidates[_random.Next(candidates.Count)];
    }
}