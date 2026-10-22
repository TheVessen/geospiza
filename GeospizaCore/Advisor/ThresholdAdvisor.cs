namespace GeospizaCore.Advisor;

/// <summary>
///     The default advisor: raise exploration on stagnation or low diversity, relax it otherwise,
///     and inject immigrants when genotypic diversity has collapsed. Also the fallback whenever
///     another advisor fails, times out or is not consulted.
/// </summary>
public sealed class ThresholdAdvisor : IRunAdvisor
{
    public const string SourceName = "thresholds";

    public static readonly ThresholdAdvisor Instance = new();

    private ThresholdAdvisor()
    {
    }

    public string Name => SourceName;

    public TimeSpan Timeout => TimeSpan.Zero;

    public Task<AdvisorDecision> AdviseAsync(RunSignals signals, CancellationToken cancellationToken)
        => Task.FromResult(Decide(signals));

    public static AdvisorDecision Decide(RunSignals signals, string? note = null)
    {
        // Without a full window, stagnation can't be judged yet, so rates stay where they are.
        var pressure = !signals.WindowReady
            ? ExplorationPressure.Hold
            : signals.IsStagnating || signals.IsDiversityLow
                ? ExplorationPressure.Raise
                : ExplorationPressure.Relax;

        return new AdvisorDecision(pressure, signals.IsDiversityCollapsed, SourceName, note: note);
    }
}
