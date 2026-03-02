"use strict";
var ChartHopper = (() => {
  var __defProp = Object.defineProperty;
  var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
  var __getOwnPropNames = Object.getOwnPropertyNames;
  var __hasOwnProp = Object.prototype.hasOwnProperty;
  var __export = (target, all) => {
    for (var name in all)
      __defProp(target, name, { get: all[name], enumerable: true });
  };
  var __copyProps = (to, from, except, desc) => {
    if (from && typeof from === "object" || typeof from === "function") {
      for (let key of __getOwnPropNames(from))
        if (!__hasOwnProp.call(to, key) && key !== except)
          __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
    }
    return to;
  };
  var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

  // src/index.ts
  var index_exports = {};
  __export(index_exports, {
    bar: () => bar,
    box: () => box,
    heatmap: () => heatmap,
    line: () => line,
    parcoords: () => parcoords,
    renderChart: () => renderChart,
    renderDashboard: () => renderDashboard,
    scatter: () => scatter,
    scatter3d: () => scatter3d
  });

  // src/core.ts
  function getCSSVariable(varName) {
    return getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
  }
  function isValidNumericArray(arr) {
    return Array.isArray(arr) && arr.length > 0 && arr.every((v) => typeof v === "number" && isFinite(v));
  }
  function isValidStringArray(arr) {
    return Array.isArray(arr) && arr.length > 0 && arr.every((v) => typeof v === "string");
  }
  function arraysMatch(...arrays) {
    if (arrays.length === 0) return false;
    const len = arrays[0].length;
    return arrays.every((a) => Array.isArray(a) && a.length === len);
  }
  function logChartError(chartType, error) {
    console.error(`[ChartHopper] ${chartType} error:`, error);
  }
  function defaultPlotlyConfig(opts) {
    return {
      responsive: true,
      displayModeBar: true,
      modeBarButtonsToRemove: ["lasso2d", "select2d"],
      toImageButtonOptions: {
        format: "svg",
        filename: "chart"
      },
      ...opts?.config
    };
  }
  function getResponsiveMargins(containerId, defaults = { t: 40, b: 50, l: 60, r: 20 }) {
    try {
      const container = document.getElementById(containerId);
      if (!container) return defaults;
      const width = container.offsetWidth || 400;
      const isMobile = width < 600;
      if (isMobile) {
        return { t: 30, b: 40, l: 40, r: 10 };
      }
      return defaults;
    } catch {
      return defaults;
    }
  }
  function buildLayout(opts) {
    const base = {
      margin: getResponsiveMargins(opts?.containerId || ""),
      font: { color: getCSSVariable("--text-primary") },
      paper_bgcolor: "transparent",
      plot_bgcolor: getCSSVariable("--plot-bg"),
      xaxis: { gridcolor: getCSSVariable("--grid-color"), zerolinecolor: getCSSVariable("--grid-zero") },
      yaxis: { gridcolor: getCSSVariable("--grid-color"), zerolinecolor: getCSSVariable("--grid-zero") }
    };
    if (opts?.title) base.title = opts.title;
    if (opts?.xAxis) base.xaxis = { ...base.xaxis ?? {}, title: opts.xAxis };
    if (opts?.yAxis) base.yaxis = { ...base.yaxis ?? {}, title: opts.yAxis };
    if (opts?.height) base.height = opts.height;
    return { ...base, ...opts?.layout };
  }
  function getPlotly() {
    return window.Plotly;
  }

  // src/charts/scatter.ts
  function scatter(containerId, cfg) {
    try {
      if (!isValidNumericArray(cfg.x) || !isValidNumericArray(cfg.y)) {
        throw new Error("x and y must be non-empty numeric arrays");
      }
      if (!arraysMatch(cfg.x, cfg.y)) {
        throw new Error("x and y arrays must have the same length");
      }
      if (cfg.color && !isValidNumericArray(cfg.color)) {
        throw new Error("color must be a numeric array matching x/y length");
      }
      if (cfg.color && !arraysMatch(cfg.x, cfg.color)) {
        throw new Error("color array length must match x/y arrays");
      }
      if (cfg.text && (!Array.isArray(cfg.text) || cfg.text.length !== cfg.x.length)) {
        throw new Error("text array length must match x/y arrays");
      }
      const P = getPlotly();
      const trace = {
        x: cfg.x,
        y: cfg.y,
        type: cfg.useWebGL ? "scattergl" : "scatter",
        mode: cfg.mode ?? "markers",
        marker: {
          size: cfg.size ?? 4,
          opacity: cfg.opacity ?? 0.7,
          ...cfg.color ? {
            color: cfg.color,
            colorscale: cfg.colorScale ?? "Viridis",
            showscale: true,
            colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : void 0
          } : {}
        }
      };
      if (cfg.text) {
        trace.text = cfg.text;
        trace.hovertemplate = "%{text}<extra></extra>";
      }
      if (cfg.customdata) {
        trace.customdata = cfg.customdata;
      }
      const layout = buildLayout({ ...cfg, containerId });
      P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));
    } catch (error) {
      logChartError("scatter", error);
      renderErrorState(containerId, error);
    }
  }
  function renderErrorState(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/charts/line.ts
  function line(containerId, cfg) {
    try {
      if (!cfg.traces || !Array.isArray(cfg.traces) || cfg.traces.length === 0) {
        throw new Error("traces must be a non-empty array");
      }
      for (let i = 0; i < cfg.traces.length; i++) {
        const trace = cfg.traces[i];
        if (!isValidNumericArray(trace.x) || !isValidNumericArray(trace.y)) {
          throw new Error(`trace ${i}: x and y must be non-empty numeric arrays`);
        }
        if (!arraysMatch(trace.x, trace.y)) {
          throw new Error(`trace ${i}: x and y arrays must have the same length`);
        }
      }
      if (cfg.fill) {
        if (!isValidNumericArray(cfg.fill.upper) || !isValidNumericArray(cfg.fill.lower) || !isValidNumericArray(cfg.fill.x)) {
          throw new Error("fill.upper, fill.lower, and fill.x must be non-empty numeric arrays");
        }
        if (!arraysMatch(cfg.fill.upper, cfg.fill.lower, cfg.fill.x)) {
          throw new Error("fill band arrays must have the same length");
        }
      }
      const P = getPlotly();
      const traces = [];
      if (cfg.fill) {
        traces.push({
          x: cfg.fill.x,
          y: cfg.fill.upper,
          type: "scatter",
          mode: "lines",
          line: { width: 0 },
          showlegend: false,
          hoverinfo: "skip"
        });
        traces.push({
          x: cfg.fill.x,
          y: cfg.fill.lower,
          type: "scatter",
          mode: "lines",
          line: { width: 0 },
          fill: "tonexty",
          fillcolor: cfg.fill.color ?? "rgba(68, 68, 68, 0.15)",
          showlegend: false,
          hoverinfo: "skip"
        });
      }
      const hoverTemplate = cfg.hoverXLabel && cfg.hoverYLabel ? `${cfg.hoverXLabel}=%{x}<br>${cfg.hoverYLabel}=%{y:.4g}<extra>%{fullData.name}</extra>` : void 0;
      for (const t of cfg.traces) {
        traces.push({
          x: t.x,
          y: t.y,
          type: "scatter",
          mode: "lines",
          name: t.name,
          line: {
            color: t.color,
            width: t.width ?? 2,
            dash: t.dash
          },
          ...hoverTemplate ? { hovertemplate: hoverTemplate } : {}
        });
      }
      const layout = {
        ...buildLayout({ ...cfg, containerId }),
        legend: { x: 1, xanchor: "right", y: 1 }
      };
      P.newPlot(containerId, traces, layout, defaultPlotlyConfig(cfg));
    } catch (error) {
      logChartError("line", error);
      renderErrorState2(containerId, error);
    }
  }
  function renderErrorState2(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/charts/bar.ts
  function bar(containerId, cfg) {
    try {
      if (!isValidStringArray(cfg.categories)) {
        throw new Error("categories must be a non-empty string array");
      }
      if (!isValidNumericArray(cfg.values)) {
        throw new Error("values must be a non-empty numeric array");
      }
      if (!arraysMatch(cfg.categories, cfg.values)) {
        throw new Error("categories and values arrays must have the same length");
      }
      if (cfg.colors && (!Array.isArray(cfg.colors) || cfg.colors.length !== cfg.values.length)) {
        throw new Error("colors array length must match values length");
      }
      const P = getPlotly();
      const isHorizontal = cfg.orientation === "h";
      const trace = {
        type: "bar",
        orientation: cfg.orientation ?? "v"
      };
      if (isHorizontal) {
        trace.x = cfg.values;
        trace.y = cfg.categories;
      } else {
        trace.x = cfg.categories;
        trace.y = cfg.values;
      }
      if (cfg.colors) {
        trace.marker = { color: cfg.colors };
      }
      const layout = {
        ...buildLayout({ ...cfg, containerId }),
        ...isHorizontal ? { yaxis: { automargin: true } } : {}
      };
      P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));
    } catch (error) {
      logChartError("bar", error);
      renderErrorState3(containerId, error);
    }
  }
  function renderErrorState3(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/charts/box.ts
  function box(containerId, cfg) {
    try {
      if (!cfg.groups || !Array.isArray(cfg.groups) || cfg.groups.length === 0) {
        throw new Error("groups must be a non-empty array");
      }
      for (let i = 0; i < cfg.groups.length; i++) {
        const group = cfg.groups[i];
        if (!group.name || typeof group.name !== "string") {
          throw new Error(`group ${i}: name must be a non-empty string`);
        }
        if (!isValidNumericArray(group.data)) {
          throw new Error(`group ${i}: data must be a non-empty numeric array`);
        }
      }
      const P = getPlotly();
      const traces = cfg.groups.map((g) => ({
        y: g.data,
        type: "box",
        name: g.name,
        marker: { size: 3, opacity: 0.5 },
        boxpoints: "outliers",
        boxmean: true,
        hovertemplate: `<b>Gen %{x}</b><br>Median: %{median:.4g}<br>Q1: %{q1:.4g} \u2014 Q3: %{q3:.4g}<br>Min: %{lowerfence:.4g} \u2014 Max: %{upperfence:.4g}<extra></extra>`
      }));
      const layout = {
        ...buildLayout({ ...cfg, containerId }),
        showlegend: false,
        xaxis: { type: "category" }
      };
      P.newPlot(containerId, traces, layout, defaultPlotlyConfig(cfg));
    } catch (error) {
      logChartError("box", error);
      renderErrorState4(containerId, error);
    }
  }
  function renderErrorState4(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/charts/parcoords.ts
  function parcoords(containerId, cfg) {
    try {
      if (!cfg.dimensions || !Array.isArray(cfg.dimensions) || cfg.dimensions.length === 0) {
        throw new Error("dimensions must be a non-empty array");
      }
      let dataLength;
      for (let i = 0; i < cfg.dimensions.length; i++) {
        const d = cfg.dimensions[i];
        if (!d.label || typeof d.label !== "string") {
          throw new Error(`dimension ${i}: label must be a non-empty string`);
        }
        if (!isValidNumericArray(d.values)) {
          throw new Error(`dimension ${i}: values must be a non-empty numeric array`);
        }
        if (dataLength === void 0) {
          dataLength = d.values.length;
        } else if (d.values.length !== dataLength) {
          throw new Error(`dimension ${i}: all dimensions must have the same value length`);
        }
      }
      if (cfg.color && !isValidNumericArray(cfg.color)) {
        throw new Error("color must be a numeric array");
      }
      if (cfg.color && cfg.color.length !== dataLength) {
        throw new Error("color array length must match dimension value length");
      }
      const P = getPlotly();
      const dimensions = cfg.dimensions.map((d) => {
        const values = d.values;
        let min = values[0];
        let max = values[0];
        for (let i = 1; i < values.length; i++) {
          if (values[i] < min) min = values[i];
          if (values[i] > max) max = values[i];
        }
        return { label: d.label, values, range: d.range ?? [min, max] };
      });
      const trace = {
        type: "parcoords",
        dimensions,
        line: cfg.color ? {
          color: cfg.color,
          colorscale: cfg.colorScale ?? "Viridis",
          reversescale: cfg.reverseScale ?? false,
          showscale: true,
          colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : void 0
        } : {}
      };
      const margin = getResponsiveMargins(containerId, {
        t: cfg.title ? 120 : 80,
        b: 60,
        l: 80,
        r: 80
      });
      const titleLayout = cfg.title ? { text: cfg.title, y: 0.99, yanchor: "top", pad: { b: 10 } } : void 0;
      const layout = {
        ...buildLayout({ ...cfg, containerId, title: void 0 }),
        title: titleLayout,
        height: cfg.height ?? 450,
        margin
      };
      P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));
      if (cfg.enableHighlight && cfg.color && cfg.color.length > 0) {
        const originalColor = [...cfg.color];
        const originalScale = cfg.colorScale ?? "Viridis";
        const originalReverse = cfg.reverseScale ?? false;
        const HIGHLIGHT_SCALE = [
          [0, "rgba(150,150,150,0.10)"],
          [1, "#ff6b35"]
        ];
        const panel = document.createElement("div");
        panel.style.cssText = "display:flex;align-items:center;gap:0.5rem;padding:0.4rem 0.75rem;font-size:0.8rem;color:var(--text-secondary);flex-wrap:wrap;";
        const btnStyle = "padding:3px 10px;border-radius:4px;border:1px solid var(--border-color);background:var(--bg-tertiary);color:var(--text-primary);cursor:pointer;font-size:0.8rem;transition:border-color 0.15s;";
        const maxIdx = originalColor.length - 1;
        panel.innerHTML = `<span>Highlight individual #</span><input type="number" id="${containerId}-hi-idx" min="0" max="${maxIdx}"  style="width:70px;padding:2px 6px;border-radius:4px;border:1px solid var(--border-color); background:var(--bg-tertiary);color:var(--text-primary);font-size:0.8rem" /><button id="${containerId}-hi-apply" style="${btnStyle}">Highlight</button><button id="${containerId}-hi-clear" style="${btnStyle}">Clear</button><span id="${containerId}-hi-label" style="color:var(--text-secondary);font-style:italic"></span>`;
        const el = document.getElementById(containerId);
        if (el && el.parentElement) {
          el.parentElement.insertBefore(panel, el);
        }
        const applyHighlight = () => {
          const input = document.getElementById(`${containerId}-hi-idx`);
          const idx = input ? parseInt(input.value, 10) : NaN;
          if (isNaN(idx) || idx < 0 || idx > maxIdx) return;
          const newColor = originalColor.map((_, i) => i === idx ? 1 : 0);
          P["restyle"](
            containerId,
            { "line.color": [newColor], "line.colorscale": [HIGHLIGHT_SCALE], "line.reversescale": [false] },
            [0]
          );
          const labelEl = document.getElementById(`${containerId}-hi-label`);
          if (labelEl) {
            const info = cfg.hoverLabels?.[idx] ?? `Individual ${idx}`;
            labelEl.textContent = `\u2192 ${info}`;
          }
        };
        const clearHighlight = () => {
          P["restyle"](
            containerId,
            {
              "line.color": [originalColor],
              "line.colorscale": [originalScale],
              "line.reversescale": [originalReverse]
            },
            [0]
          );
          const labelEl = document.getElementById(`${containerId}-hi-label`);
          if (labelEl) labelEl.textContent = "";
        };
        document.getElementById(`${containerId}-hi-apply`)?.addEventListener("click", applyHighlight);
        document.getElementById(`${containerId}-hi-clear`)?.addEventListener("click", clearHighlight);
        document.getElementById(`${containerId}-hi-idx`)?.addEventListener("keydown", (e) => {
          if (e.key === "Enter") applyHighlight();
        });
      }
    } catch (error) {
      logChartError("parcoords", error);
      renderErrorState5(containerId, error);
    }
  }
  function renderErrorState5(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/charts/scatter3d.ts
  function scatter3d(containerId, cfg) {
    try {
      if (!isValidNumericArray(cfg.x) || !isValidNumericArray(cfg.y) || !isValidNumericArray(cfg.z)) {
        throw new Error("x, y, and z must be non-empty numeric arrays");
      }
      if (!arraysMatch(cfg.x, cfg.y, cfg.z)) {
        throw new Error("x, y, and z arrays must have the same length");
      }
      if (cfg.color && !isValidNumericArray(cfg.color)) {
        throw new Error("color must be a numeric array matching x/y/z length");
      }
      if (cfg.color && !arraysMatch(cfg.x, cfg.color)) {
        throw new Error("color array length must match x/y/z arrays");
      }
      if (cfg.text && (!Array.isArray(cfg.text) || cfg.text.length !== cfg.x.length)) {
        throw new Error("text array length must match x/y/z arrays");
      }
      const P = getPlotly();
      const trace = {
        x: cfg.x,
        y: cfg.y,
        z: cfg.z,
        type: "scatter3d",
        mode: "markers",
        marker: {
          size: cfg.size ?? 4,
          ...cfg.color ? {
            color: cfg.color,
            colorscale: cfg.colorScale ?? "Viridis",
            showscale: true,
            colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : void 0
          } : {}
        }
      };
      if (cfg.text) trace.text = cfg.text;
      const margin = getResponsiveMargins(containerId, { t: 40, b: 20, l: 20, r: 20 });
      const layout = {
        ...buildLayout({ ...cfg, containerId }),
        height: cfg.height ?? 600,
        margin,
        scene: {
          xaxis: { title: cfg.xAxis },
          yaxis: { title: cfg.yAxis },
          zaxis: { title: cfg.zAxis }
        }
      };
      P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));
    } catch (error) {
      logChartError("scatter3d", error);
      renderErrorState6(containerId, error);
    }
  }
  function renderErrorState6(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/charts/heatmap.ts
  function heatmap(containerId, cfg) {
    try {
      if (!Array.isArray(cfg.z) || cfg.z.length === 0) {
        throw new Error("z must be a non-empty 2D array");
      }
      const colCount = cfg.z[0].length;
      if (colCount === 0) {
        throw new Error("z rows must not be empty");
      }
      for (let i = 0; i < cfg.z.length; i++) {
        const row = cfg.z[i];
        if (!Array.isArray(row) || row.length !== colCount) {
          throw new Error(`z row ${i}: all rows must have the same length`);
        }
        if (!row.every((v) => typeof v === "number" && isFinite(v))) {
          throw new Error(`z row ${i}: all values must be finite numbers`);
        }
      }
      if (cfg.x && (!isValidStringArray(cfg.x) || cfg.x.length !== colCount)) {
        throw new Error(`x labels length must match z column count (${colCount})`);
      }
      if (cfg.y && (!isValidStringArray(cfg.y) || cfg.y.length !== cfg.z.length)) {
        throw new Error(`y labels length must match z row count (${cfg.z.length})`);
      }
      const P = getPlotly();
      const trace = {
        z: cfg.z,
        type: "heatmap",
        colorscale: cfg.colorScale ?? "Viridis",
        colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : void 0
      };
      if (cfg.x) trace.x = cfg.x;
      if (cfg.y) trace.y = cfg.y;
      P.newPlot(containerId, [trace], buildLayout({ ...cfg, containerId }), defaultPlotlyConfig(cfg));
    } catch (error) {
      logChartError("heatmap", error);
      renderErrorState7(containerId, error);
    }
  }
  function renderErrorState7(containerId, error) {
    try {
      const container = document.getElementById(containerId);
      if (container) {
        const msg = error instanceof Error ? error.message : "Failed to render chart";
        container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">\u26A0\uFE0F ${msg}</div>`;
      }
    } catch {
    }
  }

  // src/index.ts
  var chartRenderers = {
    scatter: (id, cfg) => scatter(id, cfg),
    line: (id, cfg) => line(id, cfg),
    bar: (id, cfg) => bar(id, cfg),
    box: (id, cfg) => box(id, cfg),
    parcoords: (id, cfg) => parcoords(id, cfg),
    scatter3d: (id, cfg) => scatter3d(id, cfg),
    heatmap: (id, cfg) => heatmap(id, cfg)
  };
  function renderChart(cfg) {
    const renderer = chartRenderers[cfg.type];
    if (renderer) renderer(cfg.containerId, cfg.config);
  }
  function renderDashboard(cfg) {
    for (const tab of cfg.tabs) {
      for (const chart of tab.charts) {
        renderChart(chart);
      }
    }
  }
  return __toCommonJS(index_exports);
})();
