#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GeospizaPlugin.AI;

public enum ProviderState
{
    Ready,

    /// <summary>Reachable, but the user has to pick, download or authorise a model first.</summary>
    NeedsSetup,

    /// <summary>The service cannot be reached; setup in Geospiza won't fix it.</summary>
    Unavailable
}

public sealed class ProviderStatus
{
    private ProviderStatus(ProviderState state, string message)
    {
        State = state;
        Message = message;
    }

    public ProviderState State { get; }
    public string Message { get; }
    public bool IsReady => State == ProviderState.Ready;

    public static ProviderStatus Ready(string message = "Ready") => new(ProviderState.Ready, message);
    public static ProviderStatus NeedsSetup(string message) => new(ProviderState.NeedsSetup, message);
    public static ProviderStatus Unavailable(string message) => new(ProviderState.Unavailable, message);
}

/// <summary>
///     A model that writes text (an LLM). Used for run analysis and canvas summaries, and through
///     <see cref="TextModelDecisionAdapter" /> for structured decisions.
/// </summary>
public interface ITextModel
{
    /// <summary>Provider plus model, e.g. "ollama/gemma4:e4b". Part of cache keys, so it changes with the model.</summary>
    string Id { get; }

    Task<ProviderStatus> CheckAsync(CancellationToken ct);

    /// <param name="jsonOutput">Ask for a single JSON object; providers that can enforce this do.</param>
    Task<string> GenerateAsync(string? system, string prompt, bool jsonOutput, CancellationToken ct);
}

/// <summary>
///     A model that answers typed questions about a state with probabilities instead of free text,
///     such as TypeSafe's Jev.
/// </summary>
public interface IDecisionModel
{
    string Id { get; }

    Task<ProviderStatus> CheckAsync(CancellationToken ct);

    /// <summary>
    ///     Throws when the model cannot answer every question; callers fall back to their own logic.
    /// </summary>
    Task<DecisionAnswers> AskAsync(JObject state, IReadOnlyList<DecisionQuestion> questions, CancellationToken ct);
}

public abstract class DecisionQuestion
{
    protected DecisionQuestion(string key, string instructions)
    {
        Key = key;
        Instructions = instructions;
    }

    /// <summary>Name the answer is returned under.</summary>
    public string Key { get; }

    public string Instructions { get; }
}

/// <summary>Picks exactly one option.</summary>
public sealed class ChoiceQuestion : DecisionQuestion
{
    /// <param name="options">Option label and when it applies, in the order presented to the model.</param>
    public ChoiceQuestion(string key, string instructions, IReadOnlyList<KeyValuePair<string, string>> options)
        : base(key, instructions)
    {
        Options = options;
    }

    public IReadOnlyList<KeyValuePair<string, string>> Options { get; }
}

/// <summary>Answered with the probability of yes.</summary>
public sealed class YesNoQuestion : DecisionQuestion
{
    public YesNoQuestion(string key, string instructions, string? yesMeans = null, string? noMeans = null)
        : base(key, instructions)
    {
        YesMeans = yesMeans;
        NoMeans = noMeans;
    }

    public string? YesMeans { get; }
    public string? NoMeans { get; }
}

public sealed class ChoiceAnswer
{
    public ChoiceAnswer(string choice, double? confidence, IReadOnlyDictionary<string, double> probabilities)
    {
        Choice = choice;
        Confidence = confidence;
        Probabilities = probabilities;
    }

    public string Choice { get; }

    /// <summary>0 when probability is spread evenly, 1 when it is all on one option; null if the model gives none.</summary>
    public double? Confidence { get; }

    public IReadOnlyDictionary<string, double> Probabilities { get; }
}

public sealed class DecisionAnswers
{
    public DecisionAnswers(string modelId)
    {
        ModelId = modelId;
    }

    /// <summary>The model that answered; may be more specific than the requested alias.</summary>
    public string ModelId { get; }

    public Dictionary<string, ChoiceAnswer> Choices { get; } = new();
    public Dictionary<string, double> YesProbabilities { get; } = new();
}
