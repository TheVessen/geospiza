namespace GeospizaCore.Core;

/// <summary>
///     Computes the exact hypervolume indicator for a Pareto front under maximization.
///     All objectives are maximized (higher = better), consistent with the rest of the codebase.
///     The reference point must be strictly dominated by at least one front member on every objective.
/// </summary>
public static class HypervolumeUtils
{
    /// <summary>
    ///     Computes the hypervolume of the given rank-0 Pareto front against <paramref name="referencePoint" />.
    ///     Uses an exact 2-D sweep for two objectives and a recursive slicing algorithm for three or more.
    /// </summary>
    /// <param name="front">Non-dominated individuals. Must have non-null <see cref="Individual.Objectives" />.</param>
    /// <param name="referencePoint">
    ///     Reference (nadir) point — must be strictly worse than the front on every objective.
    /// </param>
    /// <returns>Hypervolume value, or 0 if the front is empty or all dominated by the reference point.</returns>
    public static double Compute(List<Individual> front, double[] referencePoint)
    {
        if (front.Count == 0) return 0.0;

        var m = referencePoint.Length;

        // Extract objective arrays, filtering out individuals dominated by the reference point.
        var points = new List<double[]>(front.Count);
        foreach (var ind in front)
        {
            var obj = ind.Objectives;
            if (obj == null || obj.Length < m) continue;

            // Keep only if the individual dominates the reference point on all objectives.
            var valid = true;
            for (var k = 0; k < m; k++)
                if (obj[k] <= referencePoint[k])
                {
                    valid = false;
                    break;
                }

            if (valid) points.Add(obj);
        }

        if (points.Count == 0) return 0.0;

        return m == 2
            ? Compute2D(points, referencePoint)
            : ComputeND(points, referencePoint, m);
    }

    // -------------------------------------------------------------------------
    // 2-D exact sweep — O(n log n)
    // Sort by first objective descending (second ascending for a non-dominated front).
    // HV = sum_i  (x[i] - x[i+1]) * (y[i] - refY),  where x[n] = refX.
    // -------------------------------------------------------------------------
    private static double Compute2D(List<double[]> points, double[] refPoint)
    {
        var sorted = points.OrderByDescending(p => p[0]).ToList();

        var hv = 0.0;
        var refX = refPoint[0];
        var refY = refPoint[1];

        for (var i = 0; i < sorted.Count; i++)
        {
            var xNext = i + 1 < sorted.Count ? sorted[i + 1][0] : refX;
            hv += (sorted[i][0] - xNext) * (sorted[i][1] - refY);
        }

        return Math.Max(hv, 0.0);
    }

    // -------------------------------------------------------------------------
    // n-D recursive sweep (HSO / WFG-style) — exact for any dimension.
    // Sweep along the last objective: sort descending, accumulate slice × HV_{m-1}.
    // -------------------------------------------------------------------------
    private static double ComputeND(List<double[]> points, double[] refPoint, int m)
    {
        if (points.Count == 0) return 0.0;

        // Base cases.
        if (m == 1)
        {
            var best = double.MinValue;
            foreach (var p in points)
                if (p[0] > best)
                    best = p[0];
            return Math.Max(0.0, best - refPoint[0]);
        }

        if (m == 2) return Compute2D(points, refPoint);

        // Sort by last objective descending.
        var sorted = points.OrderByDescending(p => p[m - 1]).ToList();

        var hv = 0.0;
        var prevBound = refPoint[m - 1];

        for (var i = 0; i < sorted.Count; i++)
        {
            var sliceHeight = sorted[i][m - 1] - prevBound;
            if (sliceHeight > 0)
            {
                // Project first (i+1) points onto (m-1) dimensions.
                var projected = new List<double[]>(i + 1);
                for (var j = 0; j <= i; j++)
                {
                    var proj = new double[m - 1];
                    Array.Copy(sorted[j], proj, m - 1);
                    projected.Add(proj);
                }

                // Remove dominated projections to keep the recursion fast.
                projected = FilterNonDominated(projected, m - 1);

                var subRef = new double[m - 1];
                Array.Copy(refPoint, subRef, m - 1);

                hv += sliceHeight * ComputeND(projected, subRef, m - 1);
            }

            prevBound = sorted[i][m - 1];
        }

        return hv;
    }

    // -------------------------------------------------------------------------
    // O(n²) non-dominated filter — fine for population sizes typical in Grasshopper.
    // All objectives maximized.
    // -------------------------------------------------------------------------
    private static List<double[]> FilterNonDominated(List<double[]> points, int m)
    {
        var result = new List<double[]>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            var dominated = false;
            for (var j = 0; j < points.Count; j++)
            {
                if (i == j) continue;
                if (Dominates(points[j], points[i], m))
                {
                    dominated = true;
                    break;
                }
            }

            if (!dominated) result.Add(points[i]);
        }

        return result;
    }

    // a maximization-dominates b: a ≥ b on all objectives and strictly > on at least one.
    private static bool Dominates(double[] a, double[] b, int m)
    {
        var strictlyBetter = false;
        for (var k = 0; k < m; k++)
        {
            if (a[k] < b[k]) return false;
            if (a[k] > b[k]) strictlyBetter = true;
        }

        return strictlyBetter;
    }
}