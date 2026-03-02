import type { LineConfig, LineTrace } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  arraysMatch,
  logChartError,
} from "../core";

/**
 * Render a line chart with optional fill bands.
 * Validates trace data and provides error handling.
 */
export function line(containerId: string, cfg: LineConfig): void {
  try {
    // Validate traces
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

    // Validate fill band if present
    if (cfg.fill) {
      if (!isValidNumericArray(cfg.fill.upper) || !isValidNumericArray(cfg.fill.lower) || !isValidNumericArray(cfg.fill.x)) {
        throw new Error("fill.upper, fill.lower, and fill.x must be non-empty numeric arrays");
      }
      if (!arraysMatch(cfg.fill.upper, cfg.fill.lower, cfg.fill.x)) {
        throw new Error("fill band arrays must have the same length");
      }
    }

    const P = getPlotly();
    const traces: Record<string, unknown>[] = [];

    // Fill band (e.g., std-dev shaded area) — must come before main traces
    if (cfg.fill) {
      traces.push({
        x: cfg.fill.x,
        y: cfg.fill.upper,
        type: "scatter",
        mode: "lines",
        line: { width: 0 },
        showlegend: false,
        hoverinfo: "skip",
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
        hoverinfo: "skip",
      });
    }

    // Main traces
    const hoverTemplate =
      cfg.hoverXLabel && cfg.hoverYLabel
        ? `${cfg.hoverXLabel}=%{x}<br>${cfg.hoverYLabel}=%{y:.4g}<extra>%{fullData.name}</extra>`
        : undefined;

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
          dash: t.dash,
        },
        ...(hoverTemplate ? { hovertemplate: hoverTemplate } : {}),
      });
    }

    const layout = {
      ...buildLayout({ ...cfg, containerId }),
      legend: { x: 1, xanchor: "right", y: 1 },
    };

    P.newPlot(containerId, traces, layout, defaultPlotlyConfig(cfg));
  } catch (error) {
    logChartError("line", error);
    renderErrorState(containerId, error);
  }
}

/** Render error placeholder. */
function renderErrorState(containerId: string, error: unknown): void {
  try {
    const container = document.getElementById(containerId);
    if (container) {
      const msg = error instanceof Error ? error.message : "Failed to render chart";
      container.innerHTML = `<div style="padding: 1rem; color: var(--text-secondary); font-size: 0.875rem;">⚠️ ${msg}</div>`;
    }
  } catch {
    // Silent fail
  }
}
