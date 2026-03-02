import type { BarConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  isValidStringArray,
  arraysMatch,
  logChartError,
} from "../core";

/**
 * Render a bar chart (vertical or horizontal).
 * Validates data arrays and orientation.
 */
export function bar(containerId: string, cfg: BarConfig): void {
  try {
    // Validate required fields
    if (!isValidStringArray(cfg.categories)) {
      throw new Error("categories must be a non-empty string array");
    }

    if (!isValidNumericArray(cfg.values)) {
      throw new Error("values must be a non-empty numeric array");
    }

    if (!arraysMatch(cfg.categories, cfg.values)) {
      throw new Error("categories and values arrays must have the same length");
    }

    // Validate optional colors
    if (cfg.colors && (!Array.isArray(cfg.colors) || cfg.colors.length !== cfg.values.length)) {
      throw new Error("colors array length must match values length");
    }

    const P = getPlotly();
    const isHorizontal = cfg.orientation === "h";

    const trace: Record<string, unknown> = {
      type: "bar",
      orientation: cfg.orientation ?? "v",
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
      ...(isHorizontal ? { yaxis: { automargin: true } } : {}),
    };

    P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));
  } catch (error) {
    logChartError("bar", error);
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
