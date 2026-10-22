#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GeospizaPlugin.AI;

/// <summary>
///     Turns a Grasshopper canvas into a short natural-language description for use as extra
///     context in the AI analysis prompt. The caller reads the canvas on the UI thread
///     (<see cref="CanvasFingerprint"/> and <see cref="CanvasExtractor"/>) and passes the
///     resulting strings in, so nothing here touches the live document.
///     Summaries are cached on topology fingerprint + model + summary prompt, so slider tweaks
///     don't re-summarise; only rewiring, switching models or editing the prompt does.
/// </summary>
public static class CanvasSummarizer
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static readonly Lazy<string> _summaryPrompt = new(() => LoadPrompt("canvas-summary.md"));

    /// <summary>
    ///     Returns a cached or freshly-generated canvas summary, or null when there is nothing
    ///     to summarise or summarisation fails. Only cancellation of <paramref name="ct"/> propagates.
    /// </summary>
    public static async Task<string?> GetSummaryAsync(string canvasFingerprint, string? canvasDescription,
        ITextModel model, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(canvasFingerprint) || string.IsNullOrEmpty(canvasDescription)) return null;

        try
        {
            var key = CacheKey(canvasFingerprint, model.Id);
            var cached = CanvasSummaryCache.TryGet(key);
            if (cached != null) return cached;

            var prompt = $"{_summaryPrompt.Value}\n\n# Canvas data\n\n{canvasDescription}\n";
            var summary = await model.GenerateAsync(null, prompt, false, ct).ConfigureAwait(false);
            summary = summary?.Trim() ?? string.Empty;
            if (summary.Length == 0) return null;

            CanvasSummaryCache.Store(key, summary);
            return summary;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Summarization is opt-in extra context — never let a failure block the main analysis.
            return null;
        }
    }

    /// <summary>
    ///     Checks whether a summary is already cached, so the UI can say "ready" vs.
    ///     "summarising..." without making an LLM call.
    /// </summary>
    public static bool IsCached(string canvasFingerprint, string modelId)
    {
        if (string.IsNullOrEmpty(canvasFingerprint)) return false;
        try
        {
            return CanvasSummaryCache.TryGet(CacheKey(canvasFingerprint, modelId)) != null;
        }
        catch
        {
            return false;
        }
    }

    private static string CacheKey(string canvasFingerprint, string modelId)
    {
        var promptHash = CanvasFingerprint.Sha256Hex(_summaryPrompt.Value);
        return CanvasFingerprint.Sha256Hex($"{canvasFingerprint}\n{modelId}\n{promptHash}");
    }

    private static string LoadPrompt(string filename)
    {
        var resourceName = $"GeospizaPlugin.Src.AI.Prompts.{filename}";
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return $"[Missing prompt file: {filename}]";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
