namespace GeospizaManager.Core
{
  /// <summary>
  /// Singleton class representing Fitness.
  /// </summary>
  public class Fitness
  {
    /// <summary>
    /// Lazy instance of the Fitness class.
    /// </summary>
    private static readonly Lazy<Fitness> _instance = new (() => new Fitness());

    /// <summary>
    /// Private field to store the fitness value.
    /// </summary>
    private double _fitness;

    /// <summary>
    /// Private constructor to prevent instantiation.
    /// </summary>
    private Fitness()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the Fitness class.
    /// </summary>
    public static Fitness Instance => _instance.Value;
        
    /// <summary>
    /// Sets the fitness value.
    /// </summary>
    /// <param name="value">The fitness value to set.</param>
    public void SetFitness(double value)
    {
      _fitness = value;
    }

    /// <summary>
    /// Gets the fitness value.
    /// </summary>
    /// <returns>The current fitness value.</returns>
    public double GetFitness()
    {
      return _fitness;
    }
  }
}