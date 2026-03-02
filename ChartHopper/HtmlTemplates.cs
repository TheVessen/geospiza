namespace ChartHopper;

/// <summary>
///     Generates HTML strings for standalone charts and dashboards.
///     Both light and dark modes are supported via CSS variables and a toggle button.
///     Theme preference is persisted in localStorage and also respects the system preference on first visit.
/// </summary>
internal static class HtmlTemplates
{
    private const string PlotlyCdn = "https://cdn.plot.ly/plotly-3.3.0.min.js";

    // Shared CSS variables for both themes — identical to charthopper-viewer.html
    private const string ThemeCss = @"
    :root {
      --bg-primary: #0f1117;
      --bg-secondary: #1a1d28;
      --bg-tertiary: #252836;
      --border-color: #2d3040;
      --border-hover: #3d4055;
      --text-primary: #e0e0e0;
      --text-secondary: #8b8fa3;
      --text-heading: #ffffff;
      --plot-bg: #1a1d28;
      --grid-color: #2d3040;
      --grid-zero: #3d4055;
      --accent: #60a5fa;
      --color-1: #60a5fa; --color-2: #f87171; --color-3: #34d399;
      --color-4: #fbbf24; --color-5: #a78bfa; --color-6: #fb923c;
      --color-7: #2dd4bf; --color-8: #f472b6;
    }
    html.light {
      --bg-primary: #ffffff;
      --bg-secondary: #f9fafb;
      --bg-tertiary: #f3f4f6;
      --border-color: #e5e7eb;
      --border-hover: #d1d5db;
      --text-primary: #1f2937;
      --text-secondary: #6b7280;
      --text-heading: #111827;
      --plot-bg: #ffffff;
      --grid-color: #e5e7eb;
      --grid-zero: #d1d5db;
      --accent: #3b82f6;
      --color-1: #3b82f6; --color-2: #ef4444; --color-3: #10b981;
      --color-4: #f59e0b; --color-5: #8b5cf6; --color-6: #f97316;
      --color-7: #14b8a6; --color-8: #ec4899;
    }
    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
    html, body { transition: background-color 0.2s, color 0.2s; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: var(--bg-primary); color: var(--text-primary); min-height: 100vh;
    }
    .js-plotly-plot .plotly .main-svg { background: transparent !important; }
    .chart-panel {
      background: var(--bg-secondary); border-radius: 8px; padding: 0.5rem;
      border: 1px solid var(--border-color); margin-bottom: 1rem; transition: border-color 0.2s;
    }
    .chart-panel:hover { border-color: var(--border-hover); }
    .note-panel {
      border-left: 3px solid var(--accent); border-radius: 0 4px 4px 0;
      padding: 0.65rem 1rem; margin-bottom: 0.75rem;
      font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.55;
      background: var(--bg-tertiary);
    }
    .note-panel strong { color: var(--text-primary); font-weight: 600; }
    .note-panel em { font-style: italic; }
    #theme-toggle {
      padding: 0.35rem 0.75rem; border-radius: 6px;
      border: 1px solid var(--border-color); background: var(--bg-tertiary);
      color: var(--text-primary); cursor: pointer; font-size: 0.85rem;
      font-weight: 500; transition: all 0.2s; white-space: nowrap;
    }
    #theme-toggle:hover { border-color: var(--border-hover); }
    /* ── Individual popup sidebar ── */
    #ch-popup {
      position: fixed; top: 0; right: -400px; width: 370px; height: 100vh;
      background: var(--bg-secondary); border-left: 1px solid var(--border-color);
      box-shadow: -4px 0 20px rgba(0,0,0,0.35); overflow-y: auto; z-index: 9999;
      transition: right 0.25s cubic-bezier(.4,0,.2,1); padding-bottom: 2rem;
    }
    #ch-popup.open { right: 0; }
    #ch-popup-header {
      position: sticky; top: 0; display: flex; align-items: center;
      justify-content: space-between; padding: 0.75rem 1rem;
      background: var(--bg-tertiary); border-bottom: 1px solid var(--border-color); z-index: 1;
    }
    #ch-popup-header span { font-size: 0.9rem; font-weight: 600; color: var(--text-heading); }
    #ch-popup-close {
      background: none; border: none; color: var(--text-secondary); cursor: pointer;
      font-size: 1.1rem; line-height: 1; padding: 0.2rem 0.4rem; border-radius: 4px;
      transition: color 0.15s;
    }
    #ch-popup-close:hover { color: var(--text-primary); }
    #ch-popup-body { padding: 0.75rem 1rem; }
    .pop-section { margin-bottom: 0.85rem; }
    .pop-label {
      font-size: 0.7rem; font-weight: 600; letter-spacing: 0.06em;
      text-transform: uppercase; color: var(--text-secondary); margin-bottom: 0.2rem;
    }
    .pop-value { font-size: 0.875rem; color: var(--text-primary); }
    .pop-table { width: 100%; border-collapse: collapse; margin-top: 0.35rem; }
    .pop-table td { font-size: 0.8rem; padding: 0.2rem 0.3rem; vertical-align: top; }
    .pop-table td:first-child { color: var(--text-secondary); width: 55%; word-break: break-all; }
    .pop-table td:last-child { color: var(--text-primary); font-variant-numeric: tabular-nums; text-align: right; }
    .pop-table tr:nth-child(even) td { background: var(--bg-tertiary); border-radius: 3px; }
    .pop-divider { border: none; border-top: 1px solid var(--border-color); margin: 0.75rem 0; }
    /* ── Parcoords filtered individual list ── */
    .pc-filter-wrapper {
      border: 1px solid var(--border-color); border-radius: 0 0 6px 6px;
      border-top: none; margin-bottom: 1rem; background: var(--bg-secondary);
    }
    .pc-filter-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 0.4rem 0.75rem; border-bottom: 1px solid var(--border-color);
      font-size: 0.8rem;
    }
    .pc-filter-count { color: var(--text-secondary); }
    .pc-filter-clear {
      padding: 2px 10px; border-radius: 4px; border: 1px solid var(--border-color);
      background: var(--bg-tertiary); color: var(--text-secondary); cursor: pointer;
      font-size: 0.75rem; transition: color 0.15s, border-color 0.15s;
    }
    .pc-filter-clear:hover { color: var(--text-primary); border-color: var(--border-hover); }
    .pc-filter-list {
      max-height: 220px; overflow-y: auto; padding: 0.25rem 0;
    }
    .pc-filter-row {
      padding: 0.25rem 0.75rem; font-size: 0.78rem; color: var(--text-secondary);
      border-bottom: 1px solid var(--border-color); font-variant-numeric: tabular-nums;
    }
    .pc-filter-row:last-child { border-bottom: none; }
    .pc-filter-empty {
      padding: 0.6rem 0.75rem; font-size: 0.8rem;
      color: var(--text-secondary); font-style: italic;
    }
";

    // Shared JS: theme management + Plotly defaults wired to CSS variables
    private const string ThemeScript = @"
(function() {
  var html = document.documentElement;
  var btn  = document.getElementById('theme-toggle');

  function cssVar(name) {
    return getComputedStyle(html).getPropertyValue(name).trim();
  }

  function applyPlotlyDefaults() {
    if (typeof Plotly === 'undefined') return;
    Plotly.defaults = Plotly.defaults || {};
    Plotly.defaults.layout = {
      paper_bgcolor: 'transparent',
      plot_bgcolor:  cssVar('--plot-bg'),
      font:  { color: cssVar('--text-primary') },
      xaxis: { gridcolor: cssVar('--grid-color'), zerolinecolor: cssVar('--grid-zero') },
      yaxis: { gridcolor: cssVar('--grid-color'), zerolinecolor: cssVar('--grid-zero') },
      colorway: [
        cssVar('--color-1'), cssVar('--color-2'), cssVar('--color-3'), cssVar('--color-4'),
        cssVar('--color-5'), cssVar('--color-6'), cssVar('--color-7'), cssVar('--color-8')
      ]
    };
  }

  function redrawCharts() {
    if (typeof Plotly === 'undefined') return;
    var charts = Array.from(document.querySelectorAll('.js-plotly-plot'));
    var update = {
      plot_bgcolor:        cssVar('--plot-bg'),
      paper_bgcolor:       'transparent',
      'font.color':        cssVar('--text-primary'),
      'xaxis.gridcolor':   cssVar('--grid-color'),
      'xaxis.zerolinecolor': cssVar('--grid-zero'),
      'yaxis.gridcolor':   cssVar('--grid-color'),
      'yaxis.zerolinecolor': cssVar('--grid-zero')
    };
    // Stagger one chart per animation frame so the browser stays responsive
    charts.forEach(function(el, i) {
      setTimeout(function() { Plotly.relayout(el, update); }, i * 16);
    });
  }

  function setTheme(light) {
    if (light) {
      html.classList.add('light');
      if (btn) btn.textContent = '\u2600\ufe0f Light';
      localStorage.setItem('ch-theme', 'light');
    } else {
      html.classList.remove('light');
      if (btn) btn.textContent = '\ud83c\udf19 Dark';
      localStorage.setItem('ch-theme', 'dark');
    }
    applyPlotlyDefaults();
    redrawCharts();
  }

  // Initialise from localStorage or system preference
  var saved = localStorage.getItem('ch-theme');
  var preferLight = !saved && window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches;
  setTheme(saved === 'light' || preferLight);

  if (btn) btn.addEventListener('click', function() { setTheme(!html.classList.contains('light')); });
  window.addEventListener('load', applyPlotlyDefaults);
})();
";

    /// <summary>
    ///     Generates a standalone HTML page for a single chart.
    /// </summary>
    public static string SingleChart(string containerId, string chartType, string configJson, string? title = null)
    {
        var pageTitle = title ?? "ChartHopper";
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>{EscapeHtml(pageTitle)}</title>
<script src=""{PlotlyCdn}""></script>
<style>{ThemeCss}
.chart-container {{ padding: 1rem; }}
header {{
  display: flex; align-items: center; justify-content: space-between;
  padding: 0.75rem 1.5rem; background: var(--bg-secondary); border-bottom: 1px solid var(--border-color);
}}
header h1 {{ font-size: 1.1rem; font-weight: 600; color: var(--text-heading); }}</style>
</head>
<body>
<header>
  <h1>{EscapeHtml(pageTitle)}</h1>
  <button id=""theme-toggle"">&#127769; Dark</button>
</header>
<div class=""chart-container""><div id=""{containerId}""></div></div>
<script src=""{PlotlyCdn}""></script>
<script>{ChartHopperJsInline()}</script>
<script>ChartHopper.{chartType}('{containerId}', {configJson});</script>
<script>{ThemeScript}</script>
</body>
</html>";
    }

    /// <summary>
    ///     Generates a standalone HTML page with tabbed dashboard layout.
    /// </summary>
    public static string Dashboard(List<DashboardTab> tabs, string? title = null)
    {
        var pageTitle = title ?? "ChartHopper Dashboard";
        var tabButtons = "";
        var tabContents = "";

        for (var i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            var activeClass = i == 0 ? " active" : "";
            tabButtons += $@"<button class=""tab-btn{activeClass}"" data-tab=""tab-{i}"">{EscapeHtml(tab.Name)}</button>";

            var chartsHtml = "";
            foreach (var item in tab.Items)
            {
                if (item is DashboardChart chart)
                    chartsHtml += $@"<div class=""chart-panel""><div id=""{chart.ContainerId}""></div></div>";
                else if (item is DashboardNote note)
                    chartsHtml += $@"<div class=""note-panel"">{note.Html}</div>";
            }

            tabContents += $@"<section id=""tab-{i}"" class=""tab-content{activeClass}"">{chartsHtml}</section>";
        }

        // Build a JS map: tabCharts[i] = [{id, type, config}, ...]
        // Charts are rendered lazily per tab to avoid exhausting WebGL contexts.
        var tabChartsJs = "var tabCharts = [\n";
        for (var i = 0; i < tabs.Count; i++)
        {
            tabChartsJs += "  [\n";
            foreach (var chart in tabs[i].Charts)
                tabChartsJs += $"    {{id:'{chart.ContainerId}',type:'{chart.ChartType}',cfg:{chart.ConfigJson}}},\n";
            tabChartsJs += "  ],\n";
        }
        tabChartsJs += "];\n";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>{EscapeHtml(pageTitle)}</title>
<style>
{ThemeCss}
header {{
  display: flex; align-items: center; justify-content: space-between;
  padding: 0.75rem 1.5rem; background: var(--bg-secondary); border-bottom: 1px solid var(--border-color);
}}
header h1 {{ font-size: 1.25rem; font-weight: 600; color: var(--text-heading); }}
nav {{
  display: flex; background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color); padding: 0 1rem;
}}
.tab-btn {{
  padding: 0.7rem 1.2rem; background: transparent; border: none;
  color: var(--text-secondary); cursor: pointer; font-size: 0.9rem;
  border-bottom: 2px solid transparent; transition: all 0.2s;
}}
.tab-btn:hover {{ color: var(--text-primary); }}
.tab-btn.active {{ color: var(--accent); border-bottom-color: var(--accent); }}
.tab-content {{ display: none; padding: 1rem; }}
.tab-content.active {{ display: block; }}
</style>
</head>
<body>
<header>
  <h1>{EscapeHtml(pageTitle)}</h1>
  <button id=""theme-toggle"">&#127769; Dark</button>
</header>
<nav>{tabButtons}</nav>
<main>{tabContents}</main>
<aside id=""ch-popup"">
  <div id=""ch-popup-header"">
    <span>Individual Details</span>
    <button id=""ch-popup-close"" title=""Close"">&#x2715;</button>
  </div>
  <div id=""ch-popup-body""></div>
</aside>
<script src=""{PlotlyCdn}""></script>
<script>{ChartHopperJsInline()}</script>
<script>
{tabChartsJs}
var activeTab = 0;

// ── Popup sidebar ────────────────────────────────────────────────────────────
var chPopup     = document.getElementById('ch-popup');
var chPopupBody = document.getElementById('ch-popup-body');
document.getElementById('ch-popup-close').addEventListener('click', function() {{
  chPopup.classList.remove('open');
}});
document.addEventListener('keydown', function(e) {{
  if (e.key === 'Escape') chPopup.classList.remove('open');
}});
window._chPopupShow = function(html) {{
  chPopupBody.innerHTML = html;
  chPopup.classList.add('open');
}};

// Attach plotly_click handlers after a chart is rendered
function attachPopupHandler(containerId) {{
  var el = document.getElementById(containerId);
  if (!el) return;
  el.on('plotly_click', function(data) {{
    if (!data || !data.points || !data.points.length) return;
    var pt = data.points[0];
    var cd = pt.customdata;
    if (cd && typeof cd === 'string') {{ window._chPopupShow(cd); return; }}
    // Fallback: show basic info when no customdata is attached
    var info = '<div class=""pop-section""><div class=""pop-label"">Point</div>' +
      '<div class=""pop-value"">x: ' + pt.x + '<br>y: ' + (pt.y !== undefined ? Number(pt.y).toFixed(4) : '—') + '</div></div>';
    window._chPopupShow(info);
  }});
}}

var rendered = {{}};

// Returns true if any chart in the tab uses WebGL (scattergl / scatter3d)
function tabHasWebGL(idx) {{
  return (tabCharts[idx] || []).some(function(c) {{
    return c.type === 'scatter' && c.cfg && c.cfg.useWebGL;
  }});
}}

// Render all charts in a tab; popup handlers are attached only on first render
function renderTab(idx) {{
  var firstTime = !rendered[idx];
  rendered[idx] = true;
  (tabCharts[idx] || []).forEach(function(c) {{
    ChartHopper[c.type](c.id, c.cfg);
    if (firstTime) attachPopupHandler(c.id);
  }});
}}

// Purge Plotly charts in a tab to free WebGL contexts
function purgeTab(idx) {{
  (tabCharts[idx] || []).forEach(function(c) {{
    var el = document.getElementById(c.id);
    if (el && el.data) Plotly.purge(el);
  }});
  // Mark as un-rendered so renderTab re-draws after a purge
  delete rendered[idx];
}}

// Tab switching — purge WebGL tabs on leave, lazy-render on enter
document.querySelectorAll('.tab-btn').forEach(function(btn) {{
  btn.addEventListener('click', function() {{
    var nextId = btn.getAttribute('data-tab');
    var nextIdx = parseInt(nextId.split('-')[1], 10);
    if (nextIdx === activeTab) return;

    // Free WebGL contexts of the outgoing tab; leave SVG tabs intact
    if (tabHasWebGL(activeTab)) purgeTab(activeTab);
    chPopup.classList.remove('open');
    document.querySelectorAll('.tab-btn').forEach(function(b) {{ b.classList.remove('active'); }});
    document.querySelectorAll('.tab-content').forEach(function(c) {{ c.classList.remove('active'); }});

    // Show new tab
    btn.classList.add('active');
    document.getElementById(nextId).classList.add('active');
    activeTab = nextIdx;

    renderTab(activeTab);
    window.dispatchEvent(new Event('resize'));
  }});
}});

// Render only the first tab on load
renderTab(0);
</script>
<script>{ThemeScript}</script>
</body>
</html>";
    }

    private static string ChartHopperJsInline()
    {
        return ViewerResources.GetChartHopperJs();
    }

    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}

