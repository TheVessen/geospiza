using System;
using System.Linq;
using Geospiza.Core;

namespace Geospiza.Strategies.Mutation;

public interface IMutationStrategy
{
    
    public void Mutate(Individual individual);
    public double MutationRate { get; set; }
}

public abstract class MutationStrategy: IMutationStrategy
{
    protected readonly Random Random = new Random();
    public double MutationRate { get; set; }
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
        foreach (var t in individual.GenePool)
        {
            if (Random.NextDouble() < MutationRate)
            {
                var currentValue = t.TickValue;
                var newValue = currentValue + Random.Next(-_mutationValue, _mutationValue);
                while (newValue < 0 || newValue > t.TickCount)
                {
                    newValue = currentValue + Random.Next(-_mutationValue, _mutationValue);
                }
                t.MutatedValue(newValue);
            }
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

    /// <summary>
    /// Overrides the Mutate method from the MutationStrategy base class.
    /// This method applies a percentage-based mutation to each gene in the individual's gene pool.
    /// </summary>
    /// <param name="individual">The individual to be mutated.</param>
    public override void Mutate(Individual individual)
    {
        foreach (var t in individual.GenePool)
        {
            
            var currentValue = t.TickValue;
            var mutationAmount = (int)(currentValue * _mutationPercentage);
            var newValue = currentValue + Random.Next(-mutationAmount, mutationAmount + 1);

            while (newValue < 0 || newValue > t.TickCount)
            {
                newValue = currentValue + Random.Next(-mutationAmount, mutationAmount + 1);
            }
            
            t.MutatedValue(newValue);
        }
    }
}

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
            if (Random.NextDouble() < MutationRate)
            {
                var newValue = Random.Next(0, t.TickCount);
                t.MutatedValue(newValue);
            }
        }
    }
}

