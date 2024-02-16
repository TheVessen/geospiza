using System;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface IMutationStrategy
{
    public void Mutate(Individual individual);
}

public abstract class MutationStrategy: IMutationStrategy
{
    protected readonly Random Random = new Random();
    protected double MutationRate { get; set; }
    public abstract void Mutate(Individual individual);
}

public class FixedValueMutation : MutationStrategy
{
    private readonly int _mutationValue; // A fixed value for mutation, e.g., 2

    public FixedValueMutation(double mutationRate, int mutationValue)
    {
        MutationRate = mutationRate;
        _mutationValue = mutationValue;
    }

    public override void Mutate(Individual individual)
    {
        for (var i = 0; i < individual._genePool.Count; i++)
        {
            if (!(Random.NextDouble() < MutationRate)) continue;
            var currentValue = individual._genePool[i].TickValue;
            var newValue = currentValue + Random.Next(-_mutationValue, _mutationValue + 1);

            // Ensure the new value is within bounds
            newValue = Math.Max(0, Math.Min(newValue, individual._genePool[i].TickCount));

            individual._genePool[i].MutatedValue(newValue);
        }
    }
}

public class PercentageMutation : MutationStrategy
{

    /// <summary>
    /// Mutation in percentage eg. 0.1 for 10%
    /// </summary>
    private readonly double _mutationPercentage; 

    public PercentageMutation(double mutationRate, double mutationPercentage)
    {
        MutationRate = mutationRate;
        _mutationPercentage = mutationPercentage;
    }

    public override void Mutate(Individual individual)
    {
        foreach (var t in individual._genePool)
        {
            if (!(Random.NextDouble() < MutationRate)) continue;
            var currentValue = t.TickValue;
            var mutationAmount = (int)(currentValue * _mutationPercentage);
            var newValue = currentValue + Random.Next(-mutationAmount, mutationAmount + 1);

            // Ensure the new value is within bounds
            newValue = Math.Max(1, Math.Min(newValue, t.TickCount));

            t.MutatedValue(newValue);
        }
    }
}
