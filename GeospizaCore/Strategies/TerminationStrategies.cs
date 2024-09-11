using GeospizaManager.Core;

namespace GeospizaManager.Strategies;

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
  private readonly int ProgessRange;

  public ProgressConvergence(double threshold = 0.1, int progessRange = 5)
  {
    TerminationThreshold = threshold;
    ProgessRange = progessRange;
  }

  public override bool Evaluate(EvolutionObserver evolutionObserver)
  {
    var averageFitness = evolutionObserver.AverageFitness;
    var bestFitness = evolutionObserver.BestFitness;

    if (averageFitness.Count < ProgessRange || bestFitness.Count < ProgessRange) return false;

    var totalNormalizedDelta = 0.0;

    for (var i = 1; i <= ProgessRange; i++)
    {
      var averageDelta =
        Math.Abs(averageFitness[averageFitness.Count - i] - averageFitness[averageFitness.Count - (i + 1)]);
      var normalizedDelta = averageDelta / bestFitness[bestFitness.Count - i];
      totalNormalizedDelta += normalizedDelta;
    }

    var finalNormalizedDelta = totalNormalizedDelta / ProgessRange;

    return finalNormalizedDelta < TerminationThreshold;
  }
}

public class PopulationDiversity : TerminationStrategy
{
  public PopulationDiversity(double threshold = 1)
  {
    TerminationThreshold = threshold;
  }

  public override bool Evaluate(EvolutionObserver evolutionObserver)
  {
    var population = evolutionObserver.GetCurrentPopulation();
    var diversity = population.GetDiversity();

    return diversity <= TerminationThreshold;
  }
}

public class MaxGenerations : TerminationStrategy
{
  public MaxGenerations(int maxGenerations)
  {
    TerminationThreshold = maxGenerations;
  }

  public override bool Evaluate(EvolutionObserver evolutionObserver)
  {
    return evolutionObserver.CurrentGenerationIndex >= TerminationThreshold;
  }
}

// public class GeneDiversity : TerminationStrategy
// {
//   public GeneDiversity(double threshold = 1)
//   {
//     TerminationThreshold = threshold;
//   }
//
//   public override bool Evaluate(Observer observer)
//   {
//     var population = observer.GetCurrentPopulation();
//
//   }
// }