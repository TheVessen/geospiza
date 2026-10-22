#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeospizaCore.Advisor;

namespace GeospizaPlugin.AI;

public enum AdvisorBackend
{
    Thresholds = 0,
    DecisionModel = 1,
    TextModel = 2
}

/// <summary>
///     Lets a decision model (or a text model through <see cref="TextModelDecisionAdapter" />) choose the
///     solver's exploration after each generation. <see cref="ThresholdAdvisor" /> decides while the run
///     is too young to judge, between model calls, and whenever the model is unsure.
/// </summary>
public sealed class ModelRunAdvisor : IRunAdvisor
{
    private const string ExplorationKey = "exploration";
    private const string ImmigrantsKey = "immigrants";
    private const string Keep = "keep";
    private const string ExploreMore = "explore_more";
    private const string Refine = "refine";

    // Jev leans toward the first option of a choice, so the conservative option goes first.
    private static readonly IReadOnlyList<DecisionQuestion> Questions = new DecisionQuestion[]
    {
        new ChoiceQuestion(ExplorationKey,
            "How should the optimiser's exploration change for the next generation? Base the answer on " +
            "progress, population_variety, duplicate_designs, exploration_level, room_to_raise_exploration " +
            "and run_phase in the state.",
            new[]
            {
                new KeyValuePair<string, string>(Keep,
                    "Leave exploration as it is. Use when progress is improving, or when exploration was raised " +
                    "in the last decision and needs a generation to take effect."),
                new KeyValuePair<string, string>(ExploreMore,
                    "Search more widely. Use when progress has stalled, or population_variety is low or collapsed, " +
                    "or most designs are duplicates, unless room_to_raise_exploration is 'none'."),
                new KeyValuePair<string, string>(Refine,
                    "Search more narrowly around the best designs. Use when progress is improving and " +
                    "exploration_level is raised, or when run_phase is late and population_variety is moderate or high.")
            }),
        new YesNoQuestion(ImmigrantsKey,
            "Should some designs in the next generation be replaced by brand-new random designs?",
            "population_variety is collapsed, or population_variety is low and progress has stalled.",
            "population_variety is moderate or high, or progress is improving.")
    };

    private readonly Func<IDecisionModel> _createModel;
    private readonly int _interval;
    private readonly double _minConfidence;
    private int _lastConsultedGeneration = int.MinValue;

    /// <param name="createModel">
    ///     Called on every consultation so AI Settings changes apply from the next generation on.
    /// </param>
    /// <param name="interval">Consult the model every this many generations; thresholds decide in between.</param>
    /// <param name="minConfidence">Below this choice confidence, thresholds decide instead.</param>
    public ModelRunAdvisor(Func<IDecisionModel> createModel, string name, int interval, TimeSpan timeout,
        double minConfidence)
    {
        _createModel = createModel;
        Name = name;
        _interval = Math.Max(1, interval);
        Timeout = timeout;
        _minConfidence = minConfidence;
    }

    public string Name { get; }
    public TimeSpan Timeout { get; }

    public async Task<AdvisorDecision> AdviseAsync(RunSignals signals, CancellationToken cancellationToken)
    {
        if (!signals.WindowReady)
            return ThresholdAdvisor.Decide(signals, "too early to judge");

        // The same advisor serves consecutive runs; a lower generation means a new run started.
        if (signals.Generation < _lastConsultedGeneration) _lastConsultedGeneration = int.MinValue;
        if (signals.Generation - (long)_lastConsultedGeneration < _interval)
            return ThresholdAdvisor.Decide(signals, $"between {Name} calls");
        _lastConsultedGeneration = signals.Generation;

        var model = _createModel();
        var answers = await model.AskAsync(RunStateDescriber.Describe(signals), Questions, cancellationToken)
            .ConfigureAwait(false);

        var exploration = answers.Choices[ExplorationKey];
        if (exploration.Confidence < _minConfidence)
            return ThresholdAdvisor.Decide(signals,
                $"{answers.ModelId} unsure ({exploration.Choice}, confidence {exploration.Confidence:0.00})");

        var pressure = exploration.Choice switch
        {
            ExploreMore => ExplorationPressure.Raise,
            Refine => ExplorationPressure.Relax,
            Keep => ExplorationPressure.Hold,
            _ => throw new InvalidOperationException($"{answers.ModelId} chose unknown option '{exploration.Choice}'.")
        };
        var immigrantProbability = answers.YesProbabilities[ImmigrantsKey];

        return new AdvisorDecision(pressure, immigrantProbability >= 0.5, answers.ModelId, exploration.Confidence,
            $"immigrants p={immigrantProbability:0.00}");
    }
}
