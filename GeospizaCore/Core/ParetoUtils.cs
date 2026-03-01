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
            front.Sort((a, b) => a.Objectives![m].CompareTo(b.Objectives![m]));

            front[0].SetCrowdingDistance(double.PositiveInfinity);
            front[n - 1].SetCrowdingDistance(double.PositiveInfinity);

            var range = front[n - 1].Objectives![m] - front[0].Objectives![m];
            if (range == 0) continue;

            for (var i = 1; i < n - 1; i++)
            {
                var current = front[i].CrowdingDistance;
                if (!double.IsInfinity(current))
                    front[i].SetCrowdingDistance(
                        current + (front[i + 1].Objectives![m] - front[i - 1].Objectives![m]) / range);
            }
        }
    }
}
