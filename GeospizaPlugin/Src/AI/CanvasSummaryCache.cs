using System;
using System.IO;

namespace GeospizaPlugin.AI;

/// <summary>
///     Disk-backed cache of LLM-generated canvas summaries, keyed on the topology
///     fingerprint produced by <see cref="CanvasFingerprint"/>. Survives Rhino restarts so
///     opening a familiar file doesn't re-run the summarization pass.
///     Entries are tiny (~a few KB each) so no eviction policy is implemented; if a user's
///     cache directory ever gets unwieldy they can wipe the folder by hand.
/// </summary>
public static class CanvasSummaryCache
{
    private static string CacheDir
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Geospiza", "canvas-summaries");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    ///     Returns the cached summary for <paramref name="fingerprint"/>, or null on miss
    ///     (or any I/O error — caching is best-effort).
    /// </summary>
    public static string? TryGet(string fingerprint)
    {
        if (string.IsNullOrEmpty(fingerprint)) return null;
        try
        {
            var path = PathFor(fingerprint);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Stores <paramref name="summary"/> under <paramref name="fingerprint"/>. Best-effort:
    ///     I/O errors are swallowed so a transient disk problem can't break the analysis path.
    /// </summary>
    public static void Store(string fingerprint, string summary)
    {
        if (string.IsNullOrEmpty(fingerprint) || summary == null) return;
        try
        {
            File.WriteAllText(PathFor(fingerprint), summary);
        }
        catch
        {
            // Best-effort — caching is an optimisation, not a correctness requirement.
        }
    }

    private static string PathFor(string fingerprint)
    {
        // Fingerprints from CanvasFingerprint are already hex SHA-256, so they're safe as filenames.
        return Path.Combine(CacheDir, $"{fingerprint}.txt");
    }
}
