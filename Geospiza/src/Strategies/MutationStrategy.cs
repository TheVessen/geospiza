using System;
using Geospiza.Core;

namespace Geospiza.Strategies;

public interface IMutationStrategy
{
    public void Mutate(Individual individual);
}

public abstract class MutationStrategy: IMutationStrategy
{
    protected double _mutationRate { get; set; }
    protected Random _random = new Random();
    public abstract void Mutate(Individual individual);
}

public class RandomMutation : MutationStrategy
{

    private int _mutationDistance = 5;
    public RandomMutation(double mutationRate)
    {
        _mutationRate = mutationRate;
    }

    public override void Mutate(Individual individual)
    {
        for (int i = 0; i < individual._genePool.Count; i++)
        {
            if (_random.NextDouble() < _mutationRate)
            {
                var geneMax = individual._genePool[i].TickCount;

                // Generate a mutation value within the mutation distance
                int mutation = individual._genePool[i].TickValue + _random.Next(-_mutationDistance, _mutationDistance + 1);

                // Check if mutation value is within bounds
                if (mutation >= 0 && mutation <= geneMax)
                {
                    individual._genePool[i].MutatedValue(mutation);
                }
            }
        }
    }
}


public class FixedValueMutation : MutationStrategy
{

    private readonly int _mutationValue; // A fixed value for mutation, e.g., 2

    public FixedValueMutation(double mutationRate, int mutationValue)
    {
        _mutationRate = mutationRate;
        _mutationValue = mutationValue;
    }

    public override void Mutate(Individual individual)
    {
        for (int i = 0; i < individual._genePool.Count; i++)
        {
            if (_random.NextDouble() < _mutationRate)
            {
                int currentValue = individual._genePool[i].TickValue;
                int newValue = currentValue + _random.Next(-_mutationValue, _mutationValue + 1);

                // Ensure the new value is within bounds
                newValue = Math.Max(0, Math.Min(newValue, individual._genePool[i].TickCount));

                individual._genePool[i].MutatedValue(newValue);
            }
        }
    }
}

public class PercentageMutation : MutationStrategy
{

    private readonly double _mutationPercentage; // e.g., 0.05 for 5%

    public PercentageMutation(double mutationRate, double mutationPercentage)
    {
        _mutationRate = mutationRate;
        _mutationPercentage = mutationPercentage;
    }

    public override void Mutate(Individual individual)
    {
        for (int i = 0; i < individual._genePool.Count; i++)
        {
            if (_random.NextDouble() < _mutationRate)
            {
                int currentValue = individual._genePool[i].TickValue;
                int mutationAmount = (int)(currentValue * _mutationPercentage);
                int newValue = currentValue + _random.Next(-mutationAmount, mutationAmount + 1);

                // Ensure the new value is within bounds
                newValue = Math.Max(1, Math.Min(newValue, individual._genePool[i].TickCount));

                individual._genePool[i].MutatedValue(newValue);
            }
        }
    }
}
