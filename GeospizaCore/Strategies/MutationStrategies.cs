using GeospizaCore.Core;

namespace GeospizaCore.Strategies;

public interface IMutationStrategy
{
    public double MutationRate { get; set; }
    public void Mutate(Individual individual);
}

public abstract class MutationStrategy : IMutationStrategy
{
    protected readonly Random Random = new();
    public double MutationRate { get; set; }
    public abstract void Mutate(Individual individual);
}

/// <summary>
///     Fixed-value mutation: adds or subtracts a fixed value from each gene independently
///     with given mutation probability. Useful for continuous or ordered domains.
/// </summary>
public class FixedValueMutation : MutationStrategy
{
    public FixedValueMutation(double mutationRate, int mutationValue)
    {
        MutationRate = mutationRate;
        MutationValue = mutationValue;
    }

    /// <summary>
    ///     A value that is added or subtracted from the gene value.
    /// </summary>
    private int MutationValue { get; }

    public override void Mutate(Individual individual)
    {
        if (MutationValue <= 0) return;
        foreach (var t in individual.GenePool)
        {
            if (Random.NextDouble() >= MutationRate) continue;

            var newValue = t.TickValue + Random.Next(-MutationValue, MutationValue);
            newValue = Math.Min(Math.Max(newValue, 0), t.TickCount);
            t.MutatedValue(newValue);
        }
    }
}

/// <summary>
///     Percentage-based mutation: mutates each gene by adding/subtracting a percentage of its current value.
///     Maintains relative gene scaling across different ranges.
/// </summary>
public class PercentageMutation : MutationStrategy
{
    public PercentageMutation(double mutationRate, double mutationPercentage)
    {
        MutationRate = mutationRate;
        MutationPercentage = mutationPercentage;
    }

    /// <summary>
    ///     Mutation in percentage eg. 0.1 for 10%
    /// </summary>
    private double MutationPercentage { get; }

    /// <summary>
    ///     Overrides the Mutate method from the MutationStrategy base class.
    ///     This method applies a percentage-based mutation to each gene in the individual's gene pool.
    /// </summary>
    /// <param name="individual">The individual to be mutated.</param>
    public override void Mutate(Individual individual)
    {
        foreach (var t in individual.GenePool)
        {
            if (Random.NextDouble() >= MutationRate) continue;

            var mutationAmount = Math.Max(1, (int)(t.TickValue * MutationPercentage));
            var newValue = t.TickValue + Random.Next(-mutationAmount, mutationAmount + 1);
            newValue = Math.Min(Math.Max(newValue, 0), t.TickCount);
            t.MutatedValue(newValue);
        }
    }
}

/// <summary>
///     Random mutation: replaces each gene with a uniformly random value from its valid range.
///     Provides maximum exploration and is useful for breaking out of local optima.
/// </summary>
public class RandomMutation : MutationStrategy
{
    public RandomMutation(double mutationRate)
    {
        MutationRate = mutationRate;
    }

    public override void Mutate(Individual individual)
    {
        foreach (var t in individual.GenePool)
        {
            if (Random.NextDouble() >= MutationRate) continue;
            t.MutatedValue(Random.Next(0, t.TickCount + 1));
        }
    }
}