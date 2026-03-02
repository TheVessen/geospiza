using System.Reflection;

namespace ChartHopper;

/// <summary>
///     Provides access to embedded viewer resources (HTML + JS).
/// </summary>
internal static class ViewerResources
{
    private static string? _cachedJs;

    /// <summary>
    ///     Returns the charthopper.js content as a string (for inlining in HTML).
    ///     Cached after first load.
    /// </summary>
    public static string GetChartHopperJs()
    {
        if (_cachedJs != null) return _cachedJs;

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = FindResource(assembly, "charthopper.js");

        if (stream == null)
            throw new InvalidOperationException(
                $"Embedded resource 'charthopper.js' not found. " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        _cachedJs = reader.ReadToEnd();
        return _cachedJs;
    }

    /// <summary>
    ///     Extracts the standalone HTML viewer to the specified directory.
    /// </summary>
    public static string ExtractViewer(string directoryPath)
    {
        return ExtractResource("charthopper-viewer.html", directoryPath);
    }

    /// <summary>
    ///     Extracts the JS bundle to the specified directory.
    /// </summary>
    public static string ExtractJs(string directoryPath)
    {
        return ExtractResource("charthopper.js", directoryPath);
    }

    private static string ExtractResource(string fileName, string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        var targetPath = Path.Combine(directoryPath, fileName);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = FindResource(assembly, fileName);

        if (stream == null)
            throw new InvalidOperationException(
                $"Embedded resource '{fileName}' not found. " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fs);

        return targetPath;
    }

    private static Stream? FindResource(Assembly assembly, string fileName)
    {
        var names = assembly.GetManifestResourceNames();
        foreach (var name in names)
        {
            if (name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(fileName.Replace("-", "_"), StringComparison.OrdinalIgnoreCase))
                return assembly.GetManifestResourceStream(name);
        }

        return null;
    }
}
