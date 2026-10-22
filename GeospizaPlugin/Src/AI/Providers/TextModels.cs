#nullable enable
using System;
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

/// <summary>A model served by the local Ollama app.</summary>
public sealed class OllamaTextModel : ITextModel
{
    private readonly string _model;

    public OllamaTextModel(string model)
    {
        _model = model;
    }

    public string Id => $"ollama/{_model}";

    public async Task<ProviderStatus> CheckAsync(CancellationToken ct)
    {
        if (!await OllamaService.IsAvailableAsync(ct).ConfigureAwait(false))
            return ProviderStatus.Unavailable("Ollama is not running. Install and start Ollama from https://ollama.com");
        if (!await OllamaService.HasModelAsync(_model, ct).ConfigureAwait(false))
            return ProviderStatus.NeedsSetup($"The model '{_model}' is not downloaded yet.");
        return ProviderStatus.Ready();
    }

    public Task<string> GenerateAsync(string? system, string prompt, bool jsonOutput, CancellationToken ct)
        => OllamaService.GenerateAsync(_model, prompt, ct, system, jsonOutput);
}

/// <summary>
///     Any server speaking the OpenAI chat-completions API: LM Studio, vLLM, llama.cpp server,
///     OpenRouter, OpenAI and others.
/// </summary>
public sealed class OpenAiCompatibleTextModel : ITextModel
{
    private const string Service = "LLM endpoint";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string? _apiKey;

    /// <param name="baseUrl">Up to and including the API version, e.g. "http://localhost:1234/v1".</param>
    public OpenAiCompatibleTextModel(string baseUrl, string model, string? apiKey)
    {
        _baseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        _model = (model ?? string.Empty).Trim();
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey!.Trim();
    }

    public string Id => $"{_baseUrl}/{_model}";

    public async Task<ProviderStatus> CheckAsync(CancellationToken ct)
    {
        if (_baseUrl.Length == 0 || _model.Length == 0)
            return ProviderStatus.NeedsSetup("Set the endpoint URL and model name in AI Settings.");
        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out _))
            return ProviderStatus.NeedsSetup($"'{_baseUrl}' is not a valid URL.");

        try
        {
            using var request = CreateRequest(HttpMethod.Get, "/models");
            using var resp = await Http.SendAsync(request, ct).ConfigureAwait(false);
            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return ProviderStatus.NeedsSetup($"{_baseUrl} rejected the API key.");
            // Some servers don't implement /models; a reachable server is good enough.
            return ProviderStatus.Ready();
        }
        catch (HttpRequestException ex)
        {
            return ProviderStatus.Unavailable($"Can't reach {_baseUrl}: {ex.GetBaseException().Message}");
        }
    }

    public async Task<string> GenerateAsync(string? system, string prompt, bool jsonOutput, CancellationToken ct)
    {
        var messages = new JArray();
        if (!string.IsNullOrEmpty(system))
            messages.Add(new JObject { ["role"] = "system", ["content"] = system });
        messages.Add(new JObject { ["role"] = "user", ["content"] = prompt });

        // response_format is left out: servers disagree on which variants they accept, and some
        // reject the request outright. JSON is requested in the prompt instead.
        var body = new JObject { ["model"] = _model, ["messages"] = messages, ["stream"] = false };

        using var request = CreateRequest(HttpMethod.Post, "/chat/completions");
        request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
        using var resp = await Http.SendAsync(request, ct).ConfigureAwait(false);
        await ProviderHttp.EnsureSuccessAsync(resp, Service).ConfigureAwait(false);

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
        var content = json["choices"]?[0]?["message"]?["content"];
        return content switch
        {
            null => throw new InvalidOperationException($"{Service} returned no message content."),
            // Some servers return content as a list of typed parts.
            JArray parts => string.Concat(parts.Select(p => p["text"]?.ToString() ?? string.Empty)),
            _ => content.ToString()
        };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path);
        if (_apiKey != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return request;
    }
}
