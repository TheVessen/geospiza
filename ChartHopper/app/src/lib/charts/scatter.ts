import type { ScatterConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  arraysMatch,
  logChartError,
} from "../core";

/**
 * Render a scatter plot.
 * Validates input data and provides graceful error handling.
 */
export function scatter(containerId: string, cfg: ScatterConfig): void {
  try {
    // Validate required fields
    if (!isValidNumericArray(cfg.x) || !isValidNumericArray(cfg.y)) {
      throw new Error("x and y must be non-empty numeric arrays");
    }

    if (!arraysMatch(cfg.x, cfg.y)) {
      throw new Error("x and y arrays must have the same length");
    }

    // Validate optional fields
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

    const trace: Record<string, unknown> = {
      x: cfg.x,
      y: cfg.y,
      type: cfg.useWebGL ? "scattergl" : "scatter",
      mode: cfg.mode ?? "markers",
      marker: {
        size: cfg.size ?? 4,
        opacity: cfg.opacity ?? 0.7,
        ...(cfg.color
          ? {
            color: cfg.color,
            colorscale: cfg.colorScale ?? "Viridis",
            showscale: true,
            colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : undefined,
          }
          : {}),
      },
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
