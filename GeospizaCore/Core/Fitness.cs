namespace GeospizaCore.Core;

/// <summary>
///     Singleton class representing Fitness.
/// </summary>
public class Fitness
{
    /// <summary>
    ///     Lazy instance of the Fitness class.
    /// </summary>
    private static readonly Lazy<Fitness> _instance = new(() => new Fitness());

    private double _fitness;
    private double[] _objectives = Array.Empty<double>();

    private Fitness()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the Fitness class.
    /// </summary>
    public static Fitness Instance => _instance.Value;

    public void SetFitness(double value)
    {
        _fitness = value;
    }

    /// <summary>
    ///     Get the current fitness value.
    /// </summary>
    /// <returns></returns>
    public double GetFitness()
    {
        return _fitness;
    }

    public void SetObjectives(double[] values)
    {
        _objectives = values;
    }

    public double[] GetObjectives()
    {
        return _objectives;
    }

    /// <summary>
    ///     Reset the fitness value to 0.
    /// </summary>
    public void ResetFitness()
    {
        _fitness = 0;
        _objectives = Array.Empty<double>();
    }
}