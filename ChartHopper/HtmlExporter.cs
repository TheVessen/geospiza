using System.Diagnostics;

namespace ChartHopper;

/// <summary>
///     Saves HTML content to disk and optionally opens it in the default browser.
/// </summary>
public static class HtmlExporter
{
    /// <summary>
    ///     Writes an HTML string to a file and optionally opens it in the browser.
    /// </summary>
    /// <param name="html">Complete HTML string (from ChartBuilder.Build() or DashboardBuilder.Build()).</param>
    /// <param name="directoryPath">Directory to write the file into.</param>
    /// <param name="fileName">File name without extension.</param>
    /// <param name="openInBrowser">Whether to open the file in the default browser.</param>
    /// <returns>Full path to the saved HTML file.</returns>
    public static string Export(string html, string directoryPath, string fileName = "chart",
        bool openInBrowser = true)
    {
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Combine(directoryPath, fileName + ".html");
        File.WriteAllText(filePath, html);

        if (openInBrowser) OpenInBrowser(filePath);

        return filePath;
    }

    private static void OpenInBrowser(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail — the user can manually open the HTML file.
        }
    }
}
