import type { BoxConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  logChartError,
} from "../core";

/**
 * Render a box plot showing statistical distributions.
 * Validates group data and provides error handling.
 */
export function box(containerId: string, cfg: BoxConfig): void {
  try {
    // Validate groups
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

    const traces: Record<string, unknown>[] = cfg.groups.map((g) => ({
      y: g.data,
      type: "box",
      name: g.name,
      marker: { size: 3, opacity: 0.5 },
      boxpoints: "outliers",
      boxmean: true,
      hovertemplate:
        `<b>Gen %{x}</b><br>` +
        `Median: %{median:.4g}<br>` +
        `Q1: %{q1:.4g} — Q3: %{q3:.4g}<br>` +
        `Min: %{lowerfence:.4g} — Max: %{upperfence:.4g}` +
        `<extra></extra>`,
    }));

    const layout = {
      ...buildLayout({ ...cfg, containerId }),
      showlegend: false,
      xaxis: { type: "category" },
    };

    P.newPlot(containerId, traces, layout, defaultPlotlyConfig(cfg));
  } catch (error) {
    logChartError("box", error);
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
