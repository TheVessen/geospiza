export { scatter } from "./charts/scatter";
export { line } from "./charts/line";
export { bar } from "./charts/bar";
export { box } from "./charts/box";
export { parcoords } from "./charts/parcoords";
export { scatter3d } from "./charts/scatter3d";
export { heatmap } from "./charts/heatmap";

export type {
  ChartOptions,
  ScatterConfig,
  LineConfig,
  LineTrace,
  FillBand,
  BarConfig,
  BoxConfig,
  ParcoordsConfig,
  Scatter3dConfig,
  HeatmapConfig,
  ChartRenderConfig,
  DashboardConfig,
} from "./types";

// Re-export a render function that dispatches by chart type
import type { ChartRenderConfig, DashboardConfig } from "./types";
import { scatter } from "./charts/scatter";
import { line } from "./charts/line";
import { bar } from "./charts/bar";
import { box } from "./charts/box";
import { parcoords } from "./charts/parcoords";
import { scatter3d } from "./charts/scatter3d";
import { heatmap } from "./charts/heatmap";

const chartRenderers: Record<string, (id: string, cfg: unknown) => void> = {
  scatter: (id, cfg) => scatter(id, cfg as Parameters<typeof scatter>[1]),
  line: (id, cfg) => line(id, cfg as Parameters<typeof line>[1]),
  bar: (id, cfg) => bar(id, cfg as Parameters<typeof bar>[1]),
  box: (id, cfg) => box(id, cfg as Parameters<typeof box>[1]),
  parcoords: (id, cfg) => parcoords(id, cfg as Parameters<typeof parcoords>[1]),
  scatter3d: (id, cfg) => scatter3d(id, cfg as Parameters<typeof scatter3d>[1]),
  heatmap: (id, cfg) => heatmap(id, cfg as Parameters<typeof heatmap>[1]),
};

/**
 * Render a single chart from a ChartRenderConfig (used by HTML templates).
 */
export function renderChart(cfg: ChartRenderConfig): void {
  const renderer = chartRenderers[cfg.type];
  if (renderer) renderer(cfg.containerId, cfg.config);
}

/**
 * Render a full dashboard page from DashboardConfig (used by dashboard HTML template).
 */
export function renderDashboard(cfg: DashboardConfig): void {
  for (const tab of cfg.tabs) {
    for (const chart of tab.charts) {
      renderChart(chart);
    }
  }
}
