using GeospizaManager.Core;

namespace GeospizaManager.Strategies;

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

public class FixedValueMutation : MutationStrategy
{
  public int MutationValue { get; }

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

public class PercentageMutation : MutationStrategy
{
  /// <summary>
  ///   Mutation in percentage eg. 0.1 for 10%
  /// </summary>
  public double MutationPercentage { get; }

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