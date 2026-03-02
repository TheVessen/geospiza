using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChartHopper;

/// <summary>
///     Supported chart types matching the JS ChartHopper functions.
/// </summary>
public enum ChartType
{
    Scatter,
    Line,
    Bar,
    Box,
    Parcoords,
    Scatter3d,
    Heatmap
}

/// <summary>
///     Fluent builder for generating standalone HTML with embedded Plotly charts.
///     Each Build() call returns a complete HTML string that renders a single chart.
/// </summary>
public class ChartBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    private readonly ChartType _type;
    private readonly Dictionary<string, object?> _config = new();
    private readonly List<Dictionary<string, object?>> _traces = new();
    private readonly List<Dictionary<string, object?>> _dimensions = new();
    private readonly List<Dictionary<string, object?>> _groups = new();

    public ChartBuilder(ChartType type)
    {
        _type = type;
    }

    /// <summary>
    ///     Returns the chart type for this builder.
    /// </summary>
    public ChartType GetChartType() => _type;

    public ChartBuilder Title(string title)
    {
        _config["title"] = title;
        return this;
    }

    public ChartBuilder XAxis(string label)
    {
        _config["xAxis"] = label;
        return this;
    }

    public ChartBuilder YAxis(string label)
    {
        _config["yAxis"] = label;
        return this;
    }

    public ChartBuilder ZAxis(string label)
    {
        _config["zAxis"] = label;
        return this;
    }

    public ChartBuilder Height(int px)
    {
        _config["height"] = px;
        return this;
    }

    /// <summary>
    ///     Add Plotly layout overrides (merged with defaults).
    /// </summary>
    public ChartBuilder Layout(Dictionary<string, object?> overrides)
    {
        _config["layout"] = overrides;
        return this;
    }

    // --- Scatter-specific ---

    /// <summary>
    ///     Set X/Y data for scatter plots.
    /// </summary>
    public ChartBuilder Data(IEnumerable<double> x, IEnumerable<double> y)
    {
        _config["x"] = x.ToArray();
        _config["y"] = y.ToArray();
        return this;
    }

    /// <summary>
    ///     Set Z data for 3D scatter.
    /// </summary>
    public ChartBuilder ZData(IEnumerable<double> z)
    {
        _config["z"] = z.ToArray();
        return this;
    }

    /// <summary>
    ///     Set color values for scatter/scatter3d (numeric colorscale).
    /// </summary>
    public ChartBuilder Color(IEnumerable<double> colorValues, string colorScale = "Viridis",
        string? colorBarTitle = null)
    {
        _config["color"] = colorValues.ToArray();
        _config["colorScale"] = colorScale;
        if (colorBarTitle != null) _config["colorBarTitle"] = colorBarTitle;
        return this;
    }

    public ChartBuilder MarkerSize(int size)
    {
        _config["size"] = size;
        return this;
    }

    public ChartBuilder UseWebGL(bool webgl = true)
    {
        _config["useWebGL"] = webgl;
        return this;
    }

    public ChartBuilder Opacity(double opacity)
    {
        _config["opacity"] = opacity;
        return this;
    }

    /// <summary>
    ///     Set per-point hover text labels for scatter charts.
    /// </summary>
    public ChartBuilder Text(IEnumerable<string> labels)
    {
        _config["text"] = labels.ToArray();
        return this;
    }

    /// <summary>
    ///     Attach arbitrary per-point data that is passed to click event handlers.
    ///     In the dashboard popup, each entry is a pre-rendered HTML string shown in the sidebar.
    /// </summary>
    public ChartBuilder CustomData(IEnumerable<string> data)
    {
        _config["customdata"] = data.ToArray();
        return this;
    }

    public ChartBuilder Mode(string mode)
    {
        _config["mode"] = mode;
        return this;
    }

    // --- Line-specific ---

    /// <summary>
    ///     Add a line trace.
    /// </summary>
    public ChartBuilder AddTrace(IEnumerable<double> x, IEnumerable<double> y, string name,
        string? color = null, string? dash = null, int width = 2)
    {
        _traces.Add(new Dictionary<string, object?>
        {
            ["x"] = x.ToArray(),
            ["y"] = y.ToArray(),
            ["name"] = name,
            ["color"] = color,
            ["dash"] = dash,
            ["width"] = width
        });
        return this;
    }

    /// <summary>
    ///     Convenience overload for integer X values (generation indices).
    /// </summary>
    public ChartBuilder AddTrace(IEnumerable<int> x, IReadOnlyList<double> y, string name,
        string? color = null, string? dash = null, int width = 2)
    {
        return AddTrace(x.Select(i => (double)i), y, name, color, dash, width);
    }

    /// <summary>
    ///     Convenience overload for integer Y values.
    /// </summary>
    public ChartBuilder AddTrace(IEnumerable<int> x, IReadOnlyList<int> y, string name,
        string? color = null, string? dash = null, int width = 2)
    {
        return AddTrace(x.Select(i => (double)i), y.Select(v => (double)v), name, color, dash, width);
    }

    /// <summary>
    ///     Set short axis-prefix labels for the hover tooltip on line charts.
    ///     E.g. HoverLabels("G", "F") → hover shows "G=5, F=9803.3" instead of the raw coordinates.
    /// </summary>
    public ChartBuilder LineHoverLabels(string xLabel, string yLabel)
    {
        _config["hoverXLabel"] = xLabel;
        _config["hoverYLabel"] = yLabel;
        return this;
    }

    /// <summary>
    ///     Add a shaded fill band (e.g., std-dev around mean).
    /// </summary>
    public ChartBuilder FillBand(IEnumerable<double> x, IEnumerable<double> upper, IEnumerable<double> lower,
        string fillColor = "rgba(68, 68, 68, 0.15)")
    {
        _config["fill"] = new Dictionary<string, object?>
        {
            ["x"] = x.ToArray(),
            ["upper"] = upper.ToArray(),
            ["lower"] = lower.ToArray(),
            ["color"] = fillColor
        };
        return this;
    }

    // --- Bar-specific ---

    /// <summary>
    ///     Set categories and values for bar charts.
    /// </summary>
    public ChartBuilder Categories(IEnumerable<string> categories, IEnumerable<double> values)
    {
        _config["categories"] = categories.ToArray();
        _config["values"] = values.ToArray();
        return this;
    }

    public ChartBuilder Horizontal()
    {
        _config["orientation"] = "h";
        return this;
    }

    /// <summary>
    ///     Set per-bar colors (CSS color strings).
    /// </summary>
    public ChartBuilder BarColors(IEnumerable<string> colors)
    {
        _config["colors"] = colors.ToArray();
        return this;
    }

    // --- Box-specific ---

    /// <summary>
    ///     Add a group of data for box plots.
    /// </summary>
    public ChartBuilder AddGroup(string name, IEnumerable<double> data)
    {
        _groups.Add(new Dictionary<string, object?>
        {
            ["name"] = name,
            ["data"] = data.ToArray()
        });
        return this;
    }

    // --- Parcoords-specific ---

    /// <summary>
    ///     Reverse the colorscale direction (e.g. so high fitness = red, low fitness = blue).
    /// </summary>
    public ChartBuilder ReverseScale(bool reverse = true)
    {
        _config["reverseScale"] = reverse;
        return this;
    }

    /// <summary>
    ///     Render a scrollable filtered list below the parcoords chart that shows only
    ///     individuals whose dimension values satisfy all active axis brush constraints.
    ///     <paramref name="hoverLabels" /> are shown as the row text for each individual.
    /// </summary>
    public ChartBuilder FilterList(IEnumerable<string> hoverLabels)
    {
        _config["filterList"] = true;
        _config["hoverLabels"] = hoverLabels.ToArray();
        return this;
    }

    /// <summary>
    ///     Add a dimension for parallel coordinates.
    /// </summary>
    public ChartBuilder AddDimension(string label, IEnumerable<double> values, double? rangeMin = null,
        double? rangeMax = null)
    {
        var dim = new Dictionary<string, object?>
        {
            ["label"] = label,
            ["values"] = values.ToArray()
        };
        if (rangeMin.HasValue && rangeMax.HasValue)
            dim["range"] = new[] { rangeMin.Value, rangeMax.Value };
        _dimensions.Add(dim);
        return this;
    }

    // --- Heatmap-specific ---

    /// <summary>
    ///     Set matrix data for heatmap.
    /// </summary>
    public ChartBuilder Matrix(double[][] z, IEnumerable<string>? xLabels = null,
        IEnumerable<string>? yLabels = null)
    {
        _config["z"] = z;
        if (xLabels != null) _config["x"] = xLabels.ToArray();
        if (yLabels != null) _config["y"] = yLabels.ToArray();
        return this;
    }

    /// <summary>
    ///     Builds the chart config as a JSON string (for embedding in HTML or composing in dashboards).
    /// </summary>
    public string BuildConfigJson()
    {
        var config = new Dictionary<string, object?>(_config);

        if (_traces.Count > 0) config["traces"] = _traces;
        if (_dimensions.Count > 0) config["dimensions"] = _dimensions;
        if (_groups.Count > 0) config["groups"] = _groups;

        return JsonConvert.SerializeObject(config, JsonSettings);
    }

    /// <summary>
    ///     Builds a complete standalone HTML string that renders this chart (legacy path).
    /// </summary>
    public string Build()
    {
        var chartId = "chart-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var configJson = BuildConfigJson();
        var chartType = _type.ToString().ToLowerInvariant();

        return HtmlTemplates.SingleChart(chartId, chartType, configJson,
            _config.TryGetValue("title", out var t) ? t?.ToString() : null);
    }

    /// <summary>
    ///     Registers this chart with the singleton ChartHopperServer and opens it in the browser.
    ///     The chart is wrapped in a single-tab dashboard automatically.
    /// </summary>
    /// <returns>The URL of the session.</returns>
    public string Open()
    {
        var tabName = _config.TryGetValue("title", out var t) ? t?.ToString() ?? "Chart" : "Chart";
        return new DashboardBuilder()
            .AddTab(tabName, this)
            .Open();
    }
}
