#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaPlugin.AI;

/// <summary>
///     Answers decision questions with a text model by asking for a JSON object. Text models give no
///     calibrated probabilities: choices come back without confidence, and yes/no answers carry
///     whatever probability the model writes.
/// </summary>
public sealed class TextModelDecisionAdapter : IDecisionModel
{
    private const string SystemPrompt =
        "You make structured decisions for a design-optimisation tool. Read the state, answer every " +
        "question, and reply with a single JSON object only: no prose, no code fences.";

    private readonly ITextModel _model;

    public TextModelDecisionAdapter(ITextModel model)
    {
        _model = model;
    }

    public string Id => _model.Id;

    public Task<ProviderStatus> CheckAsync(CancellationToken ct) => _model.CheckAsync(ct);

    public async Task<DecisionAnswers> AskAsync(JObject state, IReadOnlyList<DecisionQuestion> questions,
        CancellationToken ct)
    {
        var raw = await _model.GenerateAsync(SystemPrompt, BuildPrompt(state, questions), true, ct).ConfigureAwait(false);
        return ParseAnswer(raw, questions, _model.Id);
    }

    internal static string BuildPrompt(JObject state, IReadOnlyList<DecisionQuestion> questions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("State:");
        sb.AppendLine(state.ToString(Formatting.Indented));
        sb.AppendLine();
        sb.AppendLine("Questions:");
        var format = new List<string>();
        foreach (var question in questions)
        {
            sb.AppendLine($"- \"{question.Key}\": {question.Instructions}");
            switch (question)
            {
                case ChoiceQuestion choice:
                    sb.AppendLine("  Answer with exactly one of these options:");
                    foreach (var option in choice.Options)
                        sb.AppendLine($"  - \"{option.Key}\": {option.Value}");
                    format.Add($"\"{question.Key}\": \"<option>\"");
                    break;
                case YesNoQuestion yesNo:
                    sb.AppendLine("  Answer with the probability (0 to 1) that the answer is yes.");
                    if (yesNo.YesMeans != null) sb.AppendLine($"  Yes means: {yesNo.YesMeans}");
                    if (yesNo.NoMeans != null) sb.AppendLine($"  No means: {yesNo.NoMeans}");
                    format.Add($"\"{question.Key}\": <number>");
                    break;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Reply with: {{{string.Join(", ", format)}}}");
        return sb.ToString();
    }

    internal static DecisionAnswers ParseAnswer(string raw, IReadOnlyList<DecisionQuestion> questions, string modelId)
    {
        var json = ExtractJsonObject(raw)
                   ?? throw new FormatException($"{modelId} did not reply with a JSON object.");
        var answers = new DecisionAnswers(modelId);

        foreach (var question in questions)
        {
            var token = json.Properties()
                .FirstOrDefault(p => string.Equals(p.Name, question.Key, StringComparison.OrdinalIgnoreCase))?.Value
                        ?? throw new FormatException($"{modelId} did not answer '{question.Key}'.");

            switch (question)
            {
                case ChoiceQuestion choice:
                    var text = token.ToString().Trim();
                    var match = choice.Options.FirstOrDefault(o =>
                        string.Equals(o.Key, text, StringComparison.OrdinalIgnoreCase));
                    if (match.Key == null)
                        throw new FormatException(
                            $"{modelId} answered '{text}' for '{question.Key}', which is not one of " +
                            string.Join(", ", choice.Options.Select(o => o.Key)) + ".");
                    answers.Choices[question.Key] =
                        new ChoiceAnswer(match.Key, null, new Dictionary<string, double>());
                    break;
                case YesNoQuestion:
                    answers.YesProbabilities[question.Key] = ParseProbability(token)
                                                              ?? throw new FormatException(
                                                                  $"{modelId} gave no usable probability for '{question.Key}'.");
                    break;
            }
        }

        return answers;
    }

    private static double? ParseProbability(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Float:
            case JTokenType.Integer:
                var value = token.Value<double>();
                return double.IsNaN(value) ? null : Math.Max(0.0, Math.Min(1.0, value));
            case JTokenType.Boolean:
                return token.Value<bool>() ? 1.0 : 0.0;
            case JTokenType.String:
                var text = token.ToString().Trim().ToLowerInvariant();
                if (text is "yes" or "true") return 1.0;
                if (text is "no" or "false") return 0.0;
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? Math.Max(0.0, Math.Min(1.0, parsed))
                    : null;
            default:
                return null;
        }
    }

    /// <summary>The outermost {...} in the reply; tolerates code fences and stray prose around it.</summary>
    private static JObject? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        try
        {
            return JObject.Parse(raw.Substring(start, end - start + 1));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
