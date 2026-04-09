using System;
using System.Collections.Generic;
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
            var resp = await _http.GetAsync("/", ct).ConfigureAwait(false);
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
            var resp = await _http.GetAsync("/api/tags", ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
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
        var models = await ListModelsAsync(ct).ConfigureAwait(false);
        return models.Exists(m => m.StartsWith(model, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reports (statusText, overallPercent) where overallPercent is -1 when indeterminate.
    /// Overall percent is computed as sum-of-completed-bytes / sum-of-total-bytes across all layers.
    /// </summary>
    public static async Task PullModelAsync(string model, IProgress<(string status, int percent)> progress, CancellationToken ct = default)
    {
        var body = JsonConvert.SerializeObject(new { name = model, stream = true });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pull")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        // ResponseHeadersRead is critical — without it HttpClient buffers the entire
        // response body before returning, so all progress events fire at once at the end.
        var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        // Track bytes per digest so we report overall progress across all layers,
        // not per-layer (which causes the bar to snap to 100% on tiny manifest layers).
        var layerCompleted = new Dictionary<string, long>();
        var layerTotal = new Dictionary<string, long>();

        using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new System.IO.StreamReader(stream);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var obj = JObject.Parse(line);
                var status = obj["status"]?.ToString() ?? string.Empty;
                var digest = obj["digest"]?.ToString();
                var completed = obj["completed"]?.ToObject<long?>() ?? 0;
                var total = obj["total"]?.ToObject<long?>() ?? 0;

                if (!string.IsNullOrEmpty(digest) && total > 0)
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
            catch
            {
                // Non-JSON line — skip
            }
        }
    }

    public static async Task<string> GenerateAsync(string model, string prompt, CancellationToken ct = default)
    {
        var body = JsonConvert.SerializeObject(new { model, prompt, stream = false });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("/api/generate", content, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var obj = JObject.Parse(json);
        return obj["response"]?.ToString() ?? string.Empty;
    }
}
