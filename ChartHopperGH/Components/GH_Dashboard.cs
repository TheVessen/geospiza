using System;
using System.Collections.Generic;
using System.Drawing;
using ChartHopper;
using Grasshopper.Kernel;

namespace ChartHopperGH.Components;

public class GH_Dashboard : GH_Component
{
    public GH_Dashboard()
        : base("Dashboard", "Dash",
            "Combines multiple chart HTML strings into a tabbed dashboard page",
            "ChartHopper", "Layout")
    {
    }

    protected override Bitmap Icon => null;
    public override Guid ComponentGuid => new("C1A2B3D4-7777-4F5A-9B6D-2C1E3A4F5B6C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Charts", "C", "Chart HTML strings (from chart components)",
            GH_ParamAccess.list);
        pManager.AddTextParameter("Tab Names", "N",
            "Tab names (one per chart, or fewer to group charts into tabs)", GH_ParamAccess.list);
        pManager.AddTextParameter("Title", "T", "Dashboard title", GH_ParamAccess.item, "Dashboard");
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("HTML", "H", "Combined dashboard HTML string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var charts = new List<string>();
        var tabNames = new List<string>();
        var title = "";

        if (!DA.GetDataList(0, charts)) return;
        DA.GetDataList(1, tabNames);
        DA.GetData(2, ref title);

        // If no tab names provided, put each chart in its own tab
        if (tabNames.Count == 0)
            for (var i = 0; i < charts.Count; i++)
                tabNames.Add($"Chart {i + 1}");

        // Group charts by tab names: if there are fewer tabs than charts,
        // extra charts go into the last tab
        var db = new DashboardBuilder().Title(title);
        var chartIdx = 0;
        for (var t = 0; t < tabNames.Count && chartIdx < charts.Count; t++)
        {
            var tabCharts = new List<ChartBuilder>();
            // Each tab gets one chart, except the last tab gets all remaining
            var endIdx = t == tabNames.Count - 1 ? charts.Count : chartIdx + 1;
            for (var c = chartIdx; c < endIdx; c++)
            {
                // We can't easily reconstruct a ChartBuilder from HTML,
                // so we'll build the dashboard manually using HtmlTemplates directly.
                // For now, we just concatenate the chart HTML bodies.
                chartIdx = c + 1;
            }
        }

        // Since we receive pre-built HTML strings, we combine them directly
        // rather than going through ChartBuilder → DashboardBuilder.
        var html = BuildDashboardFromHtmlStrings(charts, tabNames, title);
        DA.SetData(0, html);
    }

    private static string BuildDashboardFromHtmlStrings(List<string> chartHtmls, List<string> tabNames,
        string title)
    {
        var tabButtons = "";
        var tabContents = "";

        for (var i = 0; i < tabNames.Count; i++)
        {
            var activeClass = i == 0 ? " active" : "";
            tabButtons += $@"<button class=""tab-btn{activeClass}"" data-tab=""tab-{i}"">{EscapeHtml(tabNames[i])}</button>";

            // Collect charts for this tab
            var chartsHtml = "";
            if (i < chartHtmls.Count)
            {
                // Extract the <body> content from each standalone chart HTML
                var content = ExtractBodyContent(chartHtmls[i]);
                chartsHtml += $@"<div class=""chart-panel"">{content}</div>";
            }

            // If last tab, add remaining charts
            if (i == tabNames.Count - 1)
                for (var j = i + 1; j < chartHtmls.Count; j++)
                {
                    var content = ExtractBodyContent(chartHtmls[j]);
                    chartsHtml += $@"<div class=""chart-panel"">{content}</div>";
                }

            tabContents += $@"<section id=""tab-{i}"" class=""tab-content{activeClass}"">{chartsHtml}</section>";
        }

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>{EscapeHtml(title)}</title>
<script src=""https://cdn.plot.ly/plotly-2.35.0.min.js""></script>
<style>
*, *::before, *::after {{ box-sizing: border-box; margin: 0; padding: 0; }}
body {{
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: #0f1117; color: #e0e0e0;
}}
nav {{ display: flex; background: #1a1d28; border-bottom: 1px solid #2d3040; padding: 0 1rem; }}
.tab-btn {{
  padding: 0.7rem 1.2rem; background: transparent; border: none;
  color: #8b8fa3; cursor: pointer; font-size: 0.9rem;
  border-bottom: 2px solid transparent; transition: all 0.2s;
}}
.tab-btn:hover {{ color: #e0e0e0; }}
.tab-btn.active {{ color: #60a5fa; border-bottom-color: #60a5fa; }}
.tab-content {{ display: none; padding: 1rem; }}
.tab-content.active {{ display: block; }}
.chart-panel {{
  background: #1a1d28; border-radius: 8px; padding: 0.5rem;
  border: 1px solid #2d3040; margin-bottom: 1rem;
}}
header {{ padding: 1rem 1.5rem; background: #1a1d28; border-bottom: 1px solid #2d3040; }}
header h1 {{ font-size: 1.25rem; font-weight: 600; color: #fff; }}
.js-plotly-plot .plotly .main-svg {{ background: transparent !important; }}
</style>
</head>
<body>
<header><h1>{EscapeHtml(title)}</h1></header>
<nav>{tabButtons}</nav>
<main>{tabContents}</main>
<script>
document.querySelectorAll('.tab-btn').forEach(function(btn) {{
  btn.addEventListener('click', function() {{
    document.querySelectorAll('.tab-btn').forEach(function(b) {{ b.classList.remove('active'); }});
    document.querySelectorAll('.tab-content').forEach(function(c) {{ c.classList.remove('active'); }});
    btn.classList.add('active');
    document.getElementById(btn.getAttribute('data-tab')).classList.add('active');
    window.dispatchEvent(new Event('resize'));
  }});
}});
</script>
</body>
</html>";
    }

    /// <summary>
    ///     Extracts content between body tags from a standalone chart HTML.
    ///     If no body tags found, returns the full HTML.
    /// </summary>
    private static string ExtractBodyContent(string html)
    {
        var bodyStart = html.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
        var bodyEnd = html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyStart >= 0 && bodyEnd > bodyStart)
            return html.Substring(bodyStart + 6, bodyEnd - bodyStart - 6);
        return html;
    }

    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
