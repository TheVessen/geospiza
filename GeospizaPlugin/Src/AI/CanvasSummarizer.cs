using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace GeospizaPlugin.AI;

/// <summary>
///     Two-layer pipeline that turns a Grasshopper canvas into a short natural-language
///     description for use as extra context in the AI analysis prompt.
///     <list type="number">
///         <item>Topology fingerprint via <see cref="CanvasFingerprint"/>; on a hit the cached summary is returned and no LLM call is made.</item>
///         <item>On a miss, <see cref="CanvasExtractor"/> produces structured text, the LLM summarises it under a focused system prompt, and the result is cached to disk for next time.</item>
///     </list>
///     Slider tweaks don't invalidate the cache (only topology changes do), so this is
///     usually free after the first prompt against a given canvas.
/// </summary>
public static class CanvasSummarizer
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    /// <summary>
    ///     Returns a cached or freshly-generated canvas summary, or null when the canvas
    ///     has no fitness component (nothing to summarise) or the LLM call fails.
    /// </summary>
    public static async Task<string?> GetSummaryAsync(GH_Document doc, string model, CancellationToken ct = default)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        var fingerprint = CanvasFingerprint.Compute(doc);
        var cached = CanvasSummaryCache.TryGet(fingerprint);
        if (cached != null) return cached;

        var extracted = CanvasExtractor.Extract(doc);
        if (extracted == null) return null;

        var prompt = BuildSummaryPrompt(extracted);

        try
        {
            var summary = await OllamaService.GenerateAsync(model, prompt, ct).ConfigureAwait(false);
            summary = summary?.Trim() ?? string.Empty;
            if (summary.Length == 0) return null;

            CanvasSummaryCache.Store(fingerprint, summary);
            return summary;
        }
        catch (OperationCanceledException)
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
    ///     Synchronously checks whether a summary is already cached for the current canvas.
    ///     Lets the UI layer say "ready" vs. "summarising..." without making an LLM call.
    /// </summary>
    public static bool IsCached(GH_Document doc)
    {
        if (doc == null) return false;
        var fingerprint = CanvasFingerprint.Compute(doc);
        return CanvasSummaryCache.TryGet(fingerprint) != null;
    }

    private static string BuildSummaryPrompt(string extractedCanvas)
    {
        var system = LoadPrompt("canvas-summary.md");
        return $"{system}\n\n# Canvas data\n\n{extractedCanvas}\n";
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
