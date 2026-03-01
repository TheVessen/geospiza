using GeospizaCore.Core;

namespace GeospizaCore.Strategies;

public interface ITerminationStrategy
{
    public double TerminationThreshold { get; set; }
    public bool Evaluate(EvolutionObserver evolutionObserver);
}

public abstract class TerminationStrategy : ITerminationStrategy
{
    public abstract bool Evaluate(EvolutionObserver evolutionObserver);
    public double TerminationThreshold { get; set; }
}

public class ProgressConvergence : TerminationStrategy
{
    private readonly int ProgressRange;

    public ProgressConvergence(double threshold = 0.1, int progressRange = 5)
    {
        TerminationThreshold = threshold;
        ProgressRange = progressRange;
    }

    public override bool Evaluate(EvolutionObserver evolutionObserver)
    {
        var averageFitness = evolutionObserver.AverageFitness;
        var bestFitness = evolutionObserver.BestFitness;

        // Need at least ProgressRange + 1 entries to compute ProgressRange deltas
        if (averageFitness.Count <= ProgressRange || bestFitness.Count <= ProgressRange) return false;

        var totalNormalizedDelta = 0.0;

        for (var i = 1; i <= ProgressRange; i++)
        {
            var averageDelta =
                Math.Abs(averageFitness[averageFitness.Count - i] - averageFitness[averageFitness.Count - (i + 1)]);
            var bestValue = Math.Abs(bestFitness[bestFitness.Count - i]);

            // If best fitness is zero, use the raw delta (cannot normalize)
            var normalizedDelta = bestValue > 0 ? averageDelta / bestValue : averageDelta;
            totalNormalizedDelta += normalizedDelta;
        }

        var finalNormalizedDelta = totalNormalizedDelta / ProgressRange;

        return finalNormalizedDelta < TerminationThreshold;
    }
}

/// <summary>
///     Termination strategy based on the diversity of the population
/// </summary>
public class PopulationDiversity : TerminationStrategy
{
    public PopulationDiversity(double threshold = 1)
    {
        TerminationThreshold = threshold;
    }

    public override bool Evaluate(EvolutionObserver evolutionObserver)
    {
        var population = evolutionObserver.CurrentPopulation;
        var diversity = population.GetDiversity();

        return diversity <= TerminationThreshold;
    }
}