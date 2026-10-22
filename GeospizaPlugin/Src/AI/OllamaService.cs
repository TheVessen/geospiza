#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaPlugin.AI;

public class OllamaService
{
    private static readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) };

    public static async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync("/", ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<string>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync("/api/tags", ct).ConfigureAwait(false);
            await EnsureSuccessAsync(resp).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = JObject.Parse(json);
            var names = new List<string>();
            foreach (var model in doc["models"] ?? new JArray())
                names.Add(model["name"]?.ToString() ?? string.Empty);
            return names;
        }
        catch
        {
            return new List<string>();
        }
    }

    public static async Task<bool> HasModelAsync(string model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(model)) return false;
        var wanted = NormalizeModelName(model);
        var models = await ListModelsAsync(ct).ConfigureAwait(false);
        return models.Exists(m => string.Equals(NormalizeModelName(m), wanted, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reports (statusText, overallPercent) where overallPercent is -1 when indeterminate.
    /// Overall percent is computed as sum-of-completed-bytes / sum-of-total-bytes across all layers.
    /// Throws when Ollama reports an error or the stream ends without a "success" status.
    /// </summary>
    public static async Task PullModelAsync(string model, IProgress<(string status, int percent)> progress, CancellationToken ct = default)
    {
        var body = JsonConvert.SerializeObject(new { name = model, stream = true });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/pull")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        // ResponseHeadersRead is critical — without it HttpClient buffers the entire
        // response body before returning, so all progress events fire at once at the end.
        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(resp).ConfigureAwait(false);

        // Track bytes per digest so we report overall progress across all layers,
        // not per-layer (which causes the bar to snap to 100% on tiny manifest layers).
        var layerCompleted = new Dictionary<string, long>();
        var layerTotal = new Dictionary<string, long>();
        var succeeded = false;

        using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line)) continue;

            JObject obj;
            try
            {
                obj = JObject.Parse(line);
            }
            catch
            {
                // Non-JSON line — skip
                continue;
            }

            // Pull failures arrive as {"error": "..."} lines after an HTTP 200.
            var error = obj["error"]?.ToString();
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"Ollama: {error}");

            var status = obj["status"]?.ToString() ?? string.Empty;
            if (status == "success") succeeded = true;
            var digest = obj["digest"]?.ToString();
            var completed = obj["completed"]?.ToObject<long?>() ?? 0;
            var total = obj["total"]?.ToObject<long?>() ?? 0;

            if (digest is { Length: > 0 } && total > 0)
            {
                layerCompleted[digest] = completed;
                layerTotal[digest] = total;
            }

            var sumTotal = 0L;
            var sumCompleted = 0L;
            foreach (var kvp in layerTotal) { sumTotal += kvp.Value; }
            foreach (var kvp in layerCompleted) { sumCompleted += kvp.Value; }

            var pct = sumTotal > 0 ? (int)(sumCompleted * 100L / sumTotal) : -1;
            progress?.Report((status, pct));
        }

        if (!succeeded)
            throw new IOException($"Download of '{model}' ended before Ollama reported success.");
    }

    /// <param name="jsonOutput">Constrains the reply to a JSON object (Ollama's "format": "json").</param>
    public static async Task<string> GenerateAsync(string model, string prompt, CancellationToken ct = default,
        string? system = null, bool jsonOutput = false)
    {
        var request = new JObject { ["model"] = model, ["prompt"] = prompt, ["stream"] = false };
        if (!string.IsNullOrEmpty(system)) request["system"] = system;
        if (jsonOutput) request["format"] = "json";
        var body = request.ToString(Formatting.None);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync("/api/generate", content, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(resp).ConfigureAwait(false);
        var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var obj = JObject.Parse(json);
        var error = obj["error"]?.ToString();
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException($"Ollama: {error}");
        return obj["response"]?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Throws with Ollama's own {"error": "..."} message when present (e.g. "model 'x' not found"),
    /// falling back to the HTTP status line.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;

        string? detail = null;
        try
        {
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            detail = JObject.Parse(body)["error"]?.ToString();
        }
        catch
        {
            // Body is not Ollama's JSON error shape; the status line is all we have.
        }

        throw new HttpRequestException(string.IsNullOrEmpty(detail)
            ? $"Ollama returned {(int)resp.StatusCode} {resp.ReasonPhrase}"
            : $"Ollama: {detail}");
    }

    /// <summary>
    /// Ollama treats an untagged name as ":latest". A colon before the last '/' belongs to a
    /// registry host:port, not a tag.
    /// </summary>
    private static string NormalizeModelName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.LastIndexOf(':') > trimmed.LastIndexOf('/') ? trimmed : trimmed + ":latest";
    }
}
