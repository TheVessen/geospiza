using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChartHopper;

/// <summary>
///     Marker interface for items that can appear inside a dashboard tab.
///     Implemented by <see cref="DashboardChart" /> and <see cref="DashboardNote" />.
/// </summary>
public interface IDashboardItem { }

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
    ///     Add a tab with zero or more charts.
    ///     When called with no charts the tab is left empty — use
    ///     <see cref="AddChart" /> and <see cref="AddNote" /> to populate it afterwards.
    /// </summary>
    public DashboardBuilder AddTab(string tabName, params ChartBuilder[] charts)
    {
        var tab = new DashboardTab(tabName);
        foreach (var chart in charts)
        {
            var containerId = $"ch-{_chartCounter++}";
            var (chartType, configJson) = ExtractChartInfo(chart);
            tab.Items.Add(new DashboardChart(containerId, chartType, configJson));
        }

        _tabs.Add(tab);
        return this;
    }

    /// <summary>
    ///     Appends a chart to the most recently added tab.
    /// </summary>
    public DashboardBuilder AddChart(ChartBuilder chart)
    {
        EnsureTab();
        var containerId = $"ch-{_chartCounter++}";
        var (chartType, configJson) = ExtractChartInfo(chart);
        _tabs[_tabs.Count - 1].Items.Add(new DashboardChart(containerId, chartType, configJson));
        return this;
    }

    /// <summary>
    ///     Appends a styled HTML note panel to the most recently added tab.
    ///     The <paramref name="html" /> is injected verbatim — use safe content only.
    /// </summary>
    public DashboardBuilder AddNote(string html)
    {
        EnsureTab();
        _tabs[_tabs.Count - 1].Items.Add(new DashboardNote(html));
        return this;
    }

    /// <summary>
    ///     Builds the complete dashboard HTML string (legacy path).
    /// </summary>
    public string Build()
    {
        return HtmlTemplates.Dashboard(_tabs, _title);
    }

    /// <summary>
    ///     Serialises the dashboard to the JSON format consumed by the SvelteKit app.
    /// </summary>
    public string BuildJson()
    {
        var payload = new
        {
            tabs = _tabs.Select(tab => new
            {
                name = tab.Name,
                charts = tab.Charts.Select(c => new
                {
                    type = c.ChartType,
                    containerId = c.ContainerId,
                    config = JsonConvert.DeserializeObject(c.ConfigJson)
                })
            })
        };

        return JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        });
    }

    /// <summary>
    ///     Registers this dashboard with the singleton ChartHopperServer and opens it in the browser.
    /// </summary>
    /// <returns>The URL of the session.</returns>
    public string Open()
    {
        var url = ChartHopperServer.Instance.RegisterSession(BuildJson());
        ChartHopperServer.OpenInBrowser(url);
        return url;
    }

    private void EnsureTab()
    {
        if (_tabs.Count == 0) throw new InvalidOperationException("Call AddTab before AddChart or AddNote.");
    }

    private static (string chartType, string configJson) ExtractChartInfo(ChartBuilder builder)
    {
        var configJson = builder.BuildConfigJson();
        var chartType = builder.GetChartType().ToString().ToLowerInvariant();
        return (chartType, configJson);
    }
}

/// <summary>
///     Represents a tab in a dashboard. Items may be charts or note panels, interleaved in order.
/// </summary>
public class DashboardTab
{
    public string Name { get; }

    /// <summary>Ordered content items — charts and/or note panels.</summary>
    public List<IDashboardItem> Items { get; } = new();

    /// <summary>Filtered view of chart items only, used for Plotly initialisation.</summary>
    public IEnumerable<DashboardChart> Charts => Items.OfType<DashboardChart>();

    public DashboardTab(string name)
    {
        Name = name;
    }
}

/// <summary>
///     A chart configuration ready to be rendered in a dashboard.
/// </summary>
public class DashboardChart : IDashboardItem
{
    public string ContainerId { get; }
    public string ChartType { get; }
    public string ConfigJson { get; }

    public DashboardChart(string containerId, string chartType, string configJson)
    {
        ContainerId = containerId;
        ChartType = chartType;
        ConfigJson = configJson;
    }
}

/// <summary>
///     A styled HTML text panel that can be interleaved with charts in a dashboard tab.
/// </summary>
public class DashboardNote : IDashboardItem
{
    /// <summary>Raw HTML injected into the note panel.</summary>
    public string Html { get; }

    public DashboardNote(string html)
    {
        Html = html;
    }
}
