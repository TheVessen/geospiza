namespace GeospizaManager.Core;

/// <summary>
/// Singleton class representing Fitness.
/// </summary>
public class Fitness
{
  /// <summary>
  /// Lazy instance of the Fitness class.
  /// </summary>
  private static readonly Lazy<Fitness> _instance = new(() => new Fitness());

  private double _fitness;

  private Fitness()
  {
  }

  /// <summary>
  /// Gets the singleton instance of the Fitness class.
  /// </summary>
  public static Fitness Instance => _instance.Value;

  public void SetFitness(double value)
  {
    _fitness = value;
  }

  public double GetFitness()
  {
    return _fitness;
  }

  public void ResetFitness()
  {
    _fitness = 0;
  }
}