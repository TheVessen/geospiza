namespace GeospizaCore.Core;

/// <summary>
///     Static utility class implementing NSGA-II Pareto operations.
///     All objectives are maximized (higher is better), matching the single-objective convention.
/// </summary>
public static class ParetoUtils
{
    /// <summary>
    ///     Returns true if solution <paramref name="a" /> Pareto-dominates <paramref name="b" />
    ///     under maximization: a dominates b when a is at least as good on every objective
    ///     and strictly better on at least one.
    /// </summary>
    public static bool Dominates(double[] a, double[] b)
    {
        var strictlyBetter = false;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] < b[i]) return false;
            if (a[i] > b[i]) strictlyBetter = true;
        }

        return strictlyBetter;
    }

    /// <summary>
    ///     Fast non-dominated sort (Deb et al. 2002), O(M·N²).
    ///     Assigns <see cref="Individual.ParetoRank" /> on every individual in-place
    ///     and returns the fronts in rank order (fronts[0] = Pareto-optimal).
    ///     Individuals must have non-null <see cref="Individual.Objectives" />.
    /// </summary>
    public static List<List<Individual>> FastNonDominatedSort(List<Individual> population)
    {
        var n = population.Count;
        var dominationCount = new int[n];
        var dominatedSets = new List<int>[n];
        for (var i = 0; i < n; i++) dominatedSets[i] = new List<int>();

        var frontIndices = new List<List<int>> { new() };

        for (var i = 0; i < n; i++)
        {
            var oi = population[i].Objectives;
            for (var j = 0; j < n; j++)
            {
                if (i == j) continue;
                var oj = population[j].Objectives;
                if (Dominates(oi, oj))
                    dominatedSets[i].Add(j);
                else if (Dominates(oj, oi))
                    dominationCount[i]++;
            }

            if (dominationCount[i] == 0)
            {
                population[i].SetParetoRank(0);
                frontIndices[0].Add(i);
            }
        }

        var currentFront = 0;
        while (frontIndices[currentFront].Count > 0)
        {
            var nextFront = new List<int>();
            foreach (var i in frontIndices[currentFront])
            foreach (var j in dominatedSets[i])
            {
                dominationCount[j]--;
                if (dominationCount[j] == 0)
                {
                    population[j].SetParetoRank(currentFront + 1);
                    nextFront.Add(j);
                }
            }

            currentFront++;
            frontIndices.Add(nextFront);
        }

        // Build result (skip last empty sentinel front)
        var fronts = new List<List<Individual>>(frontIndices.Count - 1);
        for (var f = 0; f < frontIndices.Count - 1; f++)
        {
            var front = new List<Individual>(frontIndices[f].Count);
            foreach (var i in frontIndices[f]) front.Add(population[i]);
            fronts.Add(front);
        }

        return fronts;
    }

    // =========================================================================
    // NSGA-III reference-point utilities (Deb & Jain 2014)
    // =========================================================================

    /// <summary>
    ///     Generates uniformly distributed reference points on the unit simplex
    ///     using the Das &amp; Dennis systematic lattice.
    ///     Produces C(<paramref name="divisions" /> + M − 1, M − 1) points, where M = <paramref name="objectiveCount" />.
    ///     Reference: I. Das and J. E. Dennis, "Normal-boundary intersection: A new method for
    ///     generating the Pareto surface in nonlinear multicriteria optimization problems,"
    ///     SIAM Journal on Optimization, vol. 8, no. 3, pp. 631–657, Aug. 1998,
    ///     doi: 10.1137/S1052623496307510.
    /// </summary>
    public static List<double[]> GenerateReferencePoints(int objectiveCount, int divisions)
    {
        var points = new List<double[]>();
        GenerateRefPointsRecursive(new double[objectiveCount], 0, divisions, divisions, points);
        return points;
    }

    private static void GenerateRefPointsRecursive(
        double[] point, int depth, int remaining, int total, List<double[]> result)
    {
        if (depth == point.Length - 1)
        {
            point[depth] = (double)remaining / total;
            result.Add((double[])point.Clone());
            return;
        }

        for (var i = 0; i <= remaining; i++)
        {
            point[depth] = (double)i / total;
            GenerateRefPointsRecursive(point, depth + 1, remaining - i, total, result);
        }
    }

    /// <summary>
    ///     Normalizes each individual's objectives for NSGA-III reference-point association.
    ///     Because all objectives are maximized, the ideal point is the per-objective maximum
    ///     and the nadir is the per-objective minimum. Each objective is translated so that
    ///     the ideal maps to 0 and divided by the range, giving normalized values in [0, 1]
    ///     where 0 = best and 1 = worst. This matches the simplex geometry used by the
    ///     reference points (Deb &amp; Jain 2014).
    ///     Returns a jagged array <c>normalized[individualIndex][objectiveIndex]</c>.
    /// </summary>
    public static double[][] NormalizeObjectives(List<Individual> population, int objectiveCount)
    {
        var n = population.Count;
        const double epsilon = 1e-10;

        // For maximization: ideal = per-objective max (best), nadir = per-objective min (worst).
        var ideal = new double[objectiveCount];
        var nadir = new double[objectiveCount];
        for (var m = 0; m < objectiveCount; m++)
        {
            ideal[m] = double.MinValue;
            nadir[m] = double.MaxValue;
        }

        foreach (var ind in population)
        {
            var obj = ind.Objectives!;
            for (var m = 0; m < objectiveCount; m++)
            {
                if (obj[m] > ideal[m]) ideal[m] = obj[m];
                if (obj[m] < nadir[m]) nadir[m] = obj[m];
            }
        }

        var range = new double[objectiveCount];
        for (var m = 0; m < objectiveCount; m++)
            range[m] = Math.Max(ideal[m] - nadir[m], epsilon);

        // Translate so ideal → 0, divide by range. Better individuals are closer to the origin.
        var normalized = new double[n][];
        for (var i = 0; i < n; i++)
        {
            var obj = population[i].Objectives!;
            normalized[i] = new double[objectiveCount];
            for (var m = 0; m < objectiveCount; m++)
                normalized[i][m] = (ideal[m] - obj[m]) / range[m];
        }

        return normalized;
    }

    /// <summary>
    ///     Associates each individual (by normalized objectives) to the nearest reference point
    ///     using perpendicular distance from the origin-to-reference-point line.
    ///     Sets <see cref="Individual.ReferencePointIndex" /> on every individual in <paramref name="population" />.
    /// </summary>
    /// <returns>
    ///     Tuple of (<c>refIndices</c>, <c>distances</c>) parallel arrays indexed by population position.
    /// </returns>
    public static (int[] refIndices, double[] distances) AssociateToReferencePoints(
        List<Individual> population, double[][] normalizedObjectives, List<double[]> referencePoints)
    {
        var n = population.Count;
        var refIndices = new int[n];
        var distances = new double[n];

        for (var i = 0; i < n; i++)
        {
            var norm = normalizedObjectives[i];
            var minDist = double.MaxValue;
            var minRef = 0;

            for (var r = 0; r < referencePoints.Count; r++)
            {
                var d = PerpendicularDistance(norm, referencePoints[r]);
                if (d < minDist)
                {
                    minDist = d;
                    minRef = r;
                }
            }

            refIndices[i] = minRef;
            distances[i] = minDist;
            population[i].SetReferencePointIndex(minRef);
        }

        return (refIndices, distances);
    }

    private static double PerpendicularDistance(double[] point, double[] refPoint)
    {
        // d = sqrt( |p|² - (p·r / |r|)² )
        var dotPR = 0.0;
        var magR2 = 0.0;
        var magP2 = 0.0;

        for (var k = 0; k < point.Length; k++)
        {
            dotPR += point[k] * refPoint[k];
            magR2 += refPoint[k] * refPoint[k];
            magP2 += point[k] * point[k];
        }

        if (magR2 < 1e-12) return Math.Sqrt(magP2);

        var proj2 = dotPR * dotPR / magR2;
        return Math.Sqrt(Math.Max(0.0, magP2 - proj2));
    }

    // =========================================================================

    /// <summary>
    ///     Assigns crowding distance to every individual in a front in-place.
    ///     Boundary individuals (per objective) receive <see cref="double.PositiveInfinity" />.
    ///     Distances are normalized per objective range and accumulated across objectives.
    /// </summary>
    public static void AssignCrowdingDistance(List<Individual> front, int objectiveCount)
    {
        var n = front.Count;
        if (n == 0) return;

        foreach (var ind in front) ind.SetCrowdingDistance(0);

        if (n <= 2)
        {
            foreach (var ind in front) ind.SetCrowdingDistance(double.PositiveInfinity);
            return;
        }

        for (var m = 0; m < objectiveCount; m++)
        {
            var sorted = front.OrderBy(ind => ind.Objectives![m]).ToList();

            sorted[0].SetCrowdingDistance(double.PositiveInfinity);
            sorted[n - 1].SetCrowdingDistance(double.PositiveInfinity);

            var range = sorted[n - 1].Objectives![m] - sorted[0].Objectives![m];
            if (range == 0) continue;

            for (var i = 1; i < n - 1; i++)
            {
                var current = sorted[i].CrowdingDistance;
                if (!double.IsInfinity(current))
                    sorted[i].SetCrowdingDistance(
                        current + (sorted[i + 1].Objectives![m] - sorted[i - 1].Objectives![m]) / range);
            }
        }
    }
}