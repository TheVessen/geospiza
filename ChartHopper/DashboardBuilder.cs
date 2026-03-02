namespace ChartHopper;

/// <summary>
///     Composes multiple charts into a tabbed HTML dashboard page.
/// </summary>
public class DashboardBuilder
{
    private readonly List<DashboardTab> _tabs = new();
    private string? _title;
    private int _chartCounter;

    /// <summary>
    ///     Set the dashboard page title.
    /// </summary>
    public DashboardBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    ///     Add a tab with one or more charts. Each chart is built via ChartBuilder.
    /// </summary>
    public DashboardBuilder AddTab(string tabName, params ChartBuilder[] charts)
    {
        var tab = new DashboardTab(tabName);
        foreach (var chart in charts)
        {
            var containerId = $"ch-{_chartCounter++}";
            var (chartType, configJson) = ExtractChartInfo(chart);
            tab.Charts.Add(new DashboardChart(containerId, chartType, configJson));
        }

        _tabs.Add(tab);
        return this;
    }

    /// <summary>
    ///     Builds the complete dashboard HTML string.
    /// </summary>
    public string Build()
    {
        return HtmlTemplates.Dashboard(_tabs, _title);
    }

    private static (string chartType, string configJson) ExtractChartInfo(ChartBuilder builder)
    {
        // We need the chart type and config JSON from the builder.
        // The builder stores these internally — we use BuildConfigJson() and infer type from the builder.
        var configJson = builder.BuildConfigJson();
        var chartType = builder.GetChartType().ToString().ToLowerInvariant();
        return (chartType, configJson);
    }
}
