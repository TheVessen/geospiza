import type { HeatmapConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  isValidStringArray,
  logChartError,
} from "../core";

/**
 * Render a heatmap with optional axis labels.
 * Validates 2D matrix data and labels.
 */
export function heatmap(containerId: string, cfg: HeatmapConfig): void {
  try {
    // Validate z matrix
    if (!Array.isArray(cfg.z) || cfg.z.length === 0) {
      throw new Error("z must be a non-empty 2D array");
    }

    const colCount = (cfg.z[0] as unknown[]).length;
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

    // Validate optional labels
    if (cfg.x && (!isValidStringArray(cfg.x) || cfg.x.length !== colCount)) {
      throw new Error(`x labels length must match z column count (${colCount})`);
    }

    if (cfg.y && (!isValidStringArray(cfg.y) || cfg.y.length !== cfg.z.length)) {
      throw new Error(`y labels length must match z row count (${cfg.z.length})`);
    }

    const P = getPlotly();

    const trace: Record<string, unknown> = {
      z: cfg.z,
      type: "heatmap",
      colorscale: cfg.colorScale ?? "Viridis",
      colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : undefined,
    };

    if (cfg.x) trace.x = cfg.x;
    if (cfg.y) trace.y = cfg.y;

    P.newPlot(containerId, [trace], buildLayout({ ...cfg, containerId }), defaultPlotlyConfig(cfg));
  } catch (error) {
    logChartError("heatmap", error);
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
