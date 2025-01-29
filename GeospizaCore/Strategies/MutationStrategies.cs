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
/// The FixedValueMutation class represents a mutation strategy that applies a fixed value mutation to each gene in an individual's gene pool. Meaning that a fixed value is added or subtracted from the gene value.
/// </summary>
public class FixedValueMutation : MutationStrategy
{
    /// <summary>
    /// A value that is added or subtracted from the gene value.
    /// </summary>
    private int MutationValue { get; }

    public FixedValueMutation(double mutationRate, int mutationValue)
    {
        MutationRate = mutationRate;
        MutationValue = mutationValue;
    }

    public override void Mutate(Individual individual)
    {
        foreach (var t in individual.GenePool)
            if (Random.NextDouble() < MutationRate)
            {
                var currentValue = t.TickValue;
                var newValue = currentValue + Random.Next(-MutationValue, MutationValue);
                while (newValue < 0 || newValue > t.TickCount)
                    newValue = currentValue + Random.Next(-MutationValue, MutationValue);

                t.MutatedValue(newValue);
            }
    }
}

/// <summary>
/// The PercentageMutation class represents a mutation strategy that applies a percentage-based mutation to each gene in an individual's gene pool. Meaning that a percentage of the gene value is added or subtracted from the gene value.
/// </summary>
public class PercentageMutation : MutationStrategy
{
    /// <summary>
    ///   Mutation in percentage eg. 0.1 for 10%
    /// </summary>
    private double MutationPercentage { get; }

    public PercentageMutation(double mutationRate, double mutationPercentage)
    {
        MutationRate = mutationRate;
        MutationPercentage = mutationPercentage;
    }

    /// <summary>
    ///   Overrides the Mutate method from the MutationStrategy base class.
    ///   This method applies a percentage-based mutation to each gene in the individual's gene pool.
    /// </summary>
    /// <param name="individual">The individual to be mutated.</param>
    public override void Mutate(Individual individual)
    {
        foreach (var t in individual.GenePool)
        {
            var currentValue = t.TickValue;
            var mutationAmount = (int)(currentValue * MutationPercentage);
            var newValue = currentValue + Random.Next(-mutationAmount, mutationAmount + 1);

            while (newValue < 0 || newValue > t.TickCount)
                newValue = currentValue + Random.Next(-mutationAmount, mutationAmount + 1);

            t.MutatedValue(newValue);
        }
    }
}

/// <summary>
/// The RandomMutation class represents a mutation strategy that applies a random mutation to each gene in an individual's gene pool. Meaning that a random value is assigned to the gene value. The amount of individuals effected by the mutation is determined by the mutation rate.
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
            if (Random.NextDouble() < MutationRate)
            {
                var newValue = Random.Next(0, t.TickCount);
                t.MutatedValue(newValue);
            }
    }
}