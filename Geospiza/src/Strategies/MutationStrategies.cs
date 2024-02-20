using System;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies.Mutation;

public interface IMutationStrategy
{
    
    public void Mutate(Individual individual);
    public double MutationRate { get; init; }
}

public abstract class MutationStrategy: IMutationStrategy
{
    protected readonly Random Random = new Random();
    public double MutationRate { get; init; }
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
        var lastGenerationMax = Observer.Instance?.CurrentPopulation.Inhabitants.Max(ind => ind.Fitness);

        for (var i = 0; i < individual.GenePool.Count; i++)
        {
            var currentValue = individual.GenePool[i].TickValue;
            var newValue = currentValue;

            // If the individual's fitness is high ranked 
            if (individual.Fitness >= lastGenerationMax * 0.8)
            {
                if (Random.NextDouble() < MutationRate)
                {
                    newValue = currentValue + Random.Next(-_mutationValue, _mutationValue + 1);
                }
            }
            // If the individual's fitness is low ranked
            else
            {
                if (Random.NextDouble() < MutationRate)
                {
                    // Multiply the mutation by a relative factor (e.g., 2)
                    newValue = currentValue + 2 * Random.Next(-_mutationValue, _mutationValue + 1);
                }
            }

            // Ensure the new value is within bounds
            newValue = Math.Max(0, Math.Min(newValue, individual.GenePool[i].TickCount));

            individual.GenePool[i].MutatedValue(newValue);
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
        foreach (var t in individual.GenePool)
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
