#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaPlugin.AI;

/// <summary>
///     TypeSafe's System One API (Jev models): typed questions about a state, answered with calibrated
///     probabilities. API reference: https://docs.typesafe.ai/api/system-one
/// </summary>
public sealed class JevDecisionModel : IDecisionModel
{
    public const string DefaultBaseUrl = "https://api.typesafe.ai";
    public const string DefaultModel = "jev-latest";

    /// <summary>The variable TypeSafe's own SDKs read.</summary>
    public const string ApiKeyEnvironmentVariable = "TYPESAFE_API_KEY";

    private const string Service = "TypeSafe";

    // TypeSafe asks clients to back off and retry on 429 (rate limit) and 529 (overloaded).
    private const int MaxAttempts = 3;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string? _apiKey;

    public JevDecisionModel(string baseUrl, string model, string? apiKey)
    {
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim().TrimEnd('/');
        _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey!.Trim();
    }

    public string Id => $"typesafe/{_model}";

    public async Task<ProviderStatus> CheckAsync(CancellationToken ct)
    {
        if (_apiKey == null)
            return ProviderStatus.NeedsSetup(
                $"No TypeSafe API key. Add one in AI Settings or set {ApiKeyEnvironmentVariable}.");

        try
        {
            using var request = CreateRequest(HttpMethod.Get, "/v1/models");
            using var resp = await Http.SendAsync(request, ct).ConfigureAwait(false);
            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return ProviderStatus.NeedsSetup("TypeSafe rejected the API key.");
            await ProviderHttp.EnsureSuccessAsync(resp, Service).ConfigureAwait(false);
            return ProviderStatus.Ready();
        }
        catch (HttpRequestException ex)
        {
            return ProviderStatus.Unavailable(ex.GetBaseException().Message);
        }
    }

    public async Task<DecisionAnswers> AskAsync(JObject state, IReadOnlyList<DecisionQuestion> questions,
        CancellationToken ct)
    {
        if (_apiKey == null)
            throw new InvalidOperationException($"No TypeSafe API key (AI Settings or {ApiKeyEnvironmentVariable}).");

        var body = BuildRequest(state, questions).ToString(Formatting.None);
        for (var attempt = 1;; attempt++)
        {
            using var request = CreateRequest(HttpMethod.Post, "/v1/systemone");
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var resp = await Http.SendAsync(request, ct).ConfigureAwait(false);

            if (((int)resp.StatusCode == 429 || (int)resp.StatusCode == 529) && attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)), ct).ConfigureAwait(false);
                continue;
            }

            await ProviderHttp.EnsureSuccessAsync(resp, Service).ConfigureAwait(false);
            var json = JObject.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
            return ParseResponse(json, questions);
        }
    }

    internal JObject BuildRequest(JObject state, IReadOnlyList<DecisionQuestion> questions)
    {
        var jsonQuestions = new JObject();
        foreach (var question in questions)
        {
            var q = new JObject { ["instructions"] = question.Instructions };
            switch (question)
            {
                case ChoiceQuestion choice:
                    q["type"] = "choice";
                    var criteria = new JObject();
                    foreach (var option in choice.Options) criteria[option.Key] = option.Value;
                    q["criteria"] = criteria;
                    break;
                case YesNoQuestion yesNo:
                    q["type"] = "noul";
                    if (yesNo.YesMeans != null && yesNo.NoMeans != null)
                        q["criteria"] = new JObject { ["true"] = yesNo.YesMeans, ["false"] = yesNo.NoMeans };
                    break;
                default:
                    throw new NotSupportedException($"Question type {question.GetType().Name} is not supported.");
            }

            jsonQuestions[question.Key] = q;
        }

        return new JObject { ["model"] = _model, ["state"] = state, ["questions"] = jsonQuestions };
    }

    internal static DecisionAnswers ParseResponse(JObject json, IReadOnlyList<DecisionQuestion> questions)
    {
        var answers = new DecisionAnswers(json["model"]?.ToString() ?? "typesafe");
        var jsonAnswers = json["answers"] as JObject
                          ?? throw new InvalidOperationException("TypeSafe response has no answers.");

        foreach (var question in questions)
        {
            var answer = jsonAnswers[question.Key] as JObject
                         ?? throw new InvalidOperationException($"TypeSafe did not answer '{question.Key}'.");
            switch (question)
            {
                case ChoiceQuestion:
                    var probabilities = (answer["probabilities"] as JObject)?.Properties()
                        .ToDictionary(p => p.Name, p => p.Value.Value<double>()) ?? new Dictionary<string, double>();
                    answers.Choices[question.Key] = new ChoiceAnswer(
                        answer["choice"]?.ToString() ?? throw new InvalidOperationException(
                            $"TypeSafe answer '{question.Key}' has no choice."),
                        answer["confidence"]?.Type == JTokenType.Float || answer["confidence"]?.Type == JTokenType.Integer
                            ? answer["confidence"]!.Value<double>()
                            : null,
                        probabilities);
                    break;
                case YesNoQuestion:
                    answers.YesProbabilities[question.Key] = answer["noul"]?.Value<double>()
                                                              ?? throw new InvalidOperationException(
                                                                  $"TypeSafe answer '{question.Key}' has no probability.");
                    break;
            }
        }

        return answers;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return request;
    }
}
