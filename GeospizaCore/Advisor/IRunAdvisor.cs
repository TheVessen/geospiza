namespace GeospizaCore.Advisor;

/// <summary>
///     Decides, after each generation, how the solver should adapt its exploration. Solvers use
///     <see cref="ThresholdAdvisor" /> unless <see cref="Solvers.SolverSettings.Advisor" /> supplies another.
/// </summary>
public interface IRunAdvisor
{
    /// <summary>Short identifier shown in the advisor log, e.g. "thresholds" or a model name.</summary>
    string Name { get; }

    /// <summary>
    ///     Longest the solver waits for <see cref="AdviseAsync" /> before applying
    ///     <see cref="ThresholdAdvisor" />'s decision instead.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    ///     Runs off the UI thread while the solver keeps Rhino responsive, so implementations must
    ///     not touch Grasshopper or Rhino objects. Exceptions fall back to <see cref="ThresholdAdvisor" />.
    /// </summary>
    Task<AdvisorDecision> AdviseAsync(RunSignals signals, CancellationToken cancellationToken);
}

/// <summary>How the solver's mutation and crossover pressure should change for the next generation.</summary>
public enum ExplorationPressure
{
    /// <summary>Leave mutation and crossover rates exactly as they are.</summary>
    Hold,

    /// <summary>Raise mutation (on top of its schedule) and crossover to search more widely.</summary>
    Raise,

    /// <summary>Let extra mutation and crossover decay back toward the schedule and base rates.</summary>
    Relax
}

public sealed class AdvisorDecision
{
    public AdvisorDecision(ExplorationPressure pressure, bool injectImmigrants, string source,
        double? confidence = null, string? note = null)
    {
        Pressure = pressure;
        InjectImmigrants = injectImmigrants;
        Source = source;
        Confidence = confidence;
        Note = note;
    }

    public ExplorationPressure Pressure { get; }

    /// <summary>Replace part of the next generation with random individuals before it is evaluated.</summary>
    public bool InjectImmigrants { get; }

    /// <summary>Who made the decision: an advisor name, or "thresholds" when falling back.</summary>
    public string Source { get; }

    /// <summary>Model confidence in <see cref="Pressure" /> (0-1), when the source reports one.</summary>
    public double? Confidence { get; }

    public string? Note { get; }
}

/// <summary>One advisor decision as recorded on the <see cref="Core.EvolutionObserver" />.</summary>
public sealed class AdvisorRecord
{
    public int Generation { get; set; }
    public string Source { get; set; } = string.Empty;
    public ExplorationPressure Pressure { get; set; }
    public bool InjectImmigrants { get; set; }
    public double? Confidence { get; set; }

    /// <summary>True when the configured advisor failed or timed out and thresholds decided instead.</summary>
    public bool FellBack { get; set; }

    public string? Note { get; set; }
    public long LatencyMs { get; set; }
}
