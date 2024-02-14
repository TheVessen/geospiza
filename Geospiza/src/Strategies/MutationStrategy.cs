using System;
using Geospiza.Core;

namespace Geospiza.Strategies;

/// <summary>
/// Blueprint for a mutation strategy
/// </summary>
public delegate Gene MutationStrategy(Gene gene);
public static class MutationStrategies
{
    
    /// <summary>
    /// BasicMutation is a delegate method that performs a basic mutation on a given gene.
    /// </summary>
    /// <param name="stableGene">The gene to be mutated.</param>
    /// <returns>A new gene with the mutated tick value.</returns>
    public static MutationStrategy BasicMutation = (Gene gene) =>
    {
        // Create a copy of the stableGene
        Gene mutatedGene = new Gene(gene);

        // Calculate the upper bound for the random number
        int upperBound = mutatedGene.TickValue < mutatedGene.TickCount
            ? mutatedGene.TickCount - mutatedGene.TickValue
            : 0;

        // Calculate the lower bound for the random number
        int lowerBound = mutatedGene.TickValue == mutatedGene.TickCount ? -5 : -upperBound;

        // Mutate the TickValue by adding a random number between lowerBound and upperBound
        mutatedGene.MutateTickValue(new Random().Next(lowerBound, upperBound + 1));

        return mutatedGene;
    };
}