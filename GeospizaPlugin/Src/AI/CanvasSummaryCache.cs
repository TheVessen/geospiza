using System;
using System.IO;
using System.Linq;

namespace GeospizaPlugin.AI;

/// <summary>
///     Disk-backed cache of LLM-generated canvas summaries, keyed on a hex SHA-256 key built by
///     <see cref="CanvasSummarizer"/>. Survives Rhino restarts so opening a familiar file doesn't
///     re-run the summarization pass. Capped at <see cref="MaxEntries"/>; least recently used
///     entries are evicted first.
/// </summary>
public static class CanvasSummaryCache
{
    private const int MaxEntries = 200;

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
    ///     Returns the cached summary for <paramref name="key"/>, or null on miss
    ///     (or any I/O error — caching is best-effort).
    /// </summary>
    public static string? TryGet(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        try
        {
            var path = PathFor(key);
            if (!File.Exists(path)) return null;
            var summary = File.ReadAllText(path);
            // Write time doubles as last-use time for eviction.
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            return summary;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Stores <paramref name="summary"/> under <paramref name="key"/>. Best-effort:
    ///     I/O errors are swallowed so a transient disk problem can't break the analysis path.
    /// </summary>
    public static void Store(string key, string summary)
    {
        if (string.IsNullOrEmpty(key) || summary == null) return;
        try
        {
            File.WriteAllText(PathFor(key), summary);
            Evict();
        }
        catch
        {
            // Best-effort — caching is an optimisation, not a correctness requirement.
        }
    }

    private static void Evict()
    {
        var stale = new DirectoryInfo(CacheDir)
            .GetFiles("*.txt")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Skip(MaxEntries);
        foreach (var file in stale)
        {
            try { file.Delete(); }
            catch { /* Another Rhino instance may hold it; retry on the next store. */ }
        }
    }

    private static string PathFor(string key)
    {
        // Keys are hex SHA-256, so they're safe as filenames.
        return Path.Combine(CacheDir, $"{key}.txt");
    }
}
