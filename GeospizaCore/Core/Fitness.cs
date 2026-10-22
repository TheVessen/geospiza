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
    private bool _fitnessWritten;
    private string[] _objectiveNames = Array.Empty<string>();
    private double[] _objectives = Array.Empty<double>();
    private bool _objectivesWritten;

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
        _fitnessWritten = true;
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
        _objectivesWritten = true;
    }

    public double[] GetObjectives()
    {
        return _objectives;
    }

    public void SetObjectiveNames(string[] names)
    {
        _objectiveNames = names;
    }

    public string[] GetObjectiveNames()
    {
        return _objectiveNames;
    }

    /// <summary>
    ///     Marks the stored values as stale before an individual is solved. A fitness component
    ///     that does not run (empty required input) or bails out leaves them stale, and
    ///     <see cref="TryGetFitness" /> / <see cref="TryGetObjectives" /> then report a failed evaluation
    ///     instead of handing out the previous individual's values.
    /// </summary>
    public void BeginEvaluation()
    {
        _fitnessWritten = false;
        _objectivesWritten = false;
    }

    /// <summary>
    ///     Gets the fitness written since <see cref="BeginEvaluation" />; false when none was written or it is not finite.
    /// </summary>
    public bool TryGetFitness(out double value)
    {
        value = _fitness;
        return _fitnessWritten && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    ///     Gets a copy of the objectives written since <see cref="BeginEvaluation" />; false when none
    ///     were written, the array is empty, or any value is not finite.
    /// </summary>
    public bool TryGetObjectives(out double[] values)
    {
        values = Array.Empty<double>();
        if (!_objectivesWritten || _objectives.Length == 0) return false;
        foreach (var v in _objectives)
            if (double.IsNaN(v) || double.IsInfinity(v))
                return false;

        values = (double[])_objectives.Clone();
        return true;
    }

    /// <summary>
    ///     Reset the fitness value to 0.
    /// </summary>
    public void ResetFitness()
    {
        _fitness = 0;
        _objectives = Array.Empty<double>();
        _objectiveNames = Array.Empty<string>();
        BeginEvaluation();
    }
}