import type { Scatter3dConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  arraysMatch,
  logChartError,
  getResponsiveMargins,
} from "../core";

/**
 * Render a 3D scatter plot with optional color mapping.
 * Validates 3D coordinates and provides error handling.
 */
export function scatter3d(containerId: string, cfg: Scatter3dConfig): void {
  try {
    // Validate required 3D coordinates
    if (!isValidNumericArray(cfg.x) || !isValidNumericArray(cfg.y) || !isValidNumericArray(cfg.z)) {
      throw new Error("x, y, and z must be non-empty numeric arrays");
    }

    if (!arraysMatch(cfg.x, cfg.y, cfg.z)) {
      throw new Error("x, y, and z arrays must have the same length");
    }

    // Validate optional fields
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

    const trace: Record<string, unknown> = {
      x: cfg.x,
      y: cfg.y,
      z: cfg.z,
      type: "scatter3d",
      mode: "markers",
      marker: {
        size: cfg.size ?? 4,
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

    if (cfg.text) trace.text = cfg.text;

    const margin = getResponsiveMargins(containerId, { t: 40, b: 20, l: 20, r: 20 });

    const layout = {
      ...buildLayout({ ...cfg, containerId }),
      height: cfg.height ?? 600,
      margin,
      scene: {
        xaxis: { title: cfg.xAxis },
        yaxis: { title: cfg.yAxis },
        zaxis: { title: cfg.zAxis },
      },
    };

    P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));
  } catch (error) {
    logChartError("scatter3d", error);
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
