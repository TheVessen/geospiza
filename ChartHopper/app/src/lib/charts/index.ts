export { scatter } from "./scatter";
export { line } from "./line";
export { bar } from "./bar";
export { box } from "./box";
export { parcoords } from "./parcoords";
export { scatter3d } from "./scatter3d";
export { heatmap } from "./heatmap";

import type { ChartRenderConfig } from "../types";
import { scatter } from "./scatter";
import { line } from "./line";
import { bar } from "./bar";
import { box } from "./box";
import { parcoords } from "./parcoords";
import { scatter3d } from "./scatter3d";
import { heatmap } from "./heatmap";

const renderers: Record<string, (id: string, cfg: unknown) => void> = {
  scatter: (id, cfg) => scatter(id, cfg as Parameters<typeof scatter>[1]),
  line: (id, cfg) => line(id, cfg as Parameters<typeof line>[1]),
  bar: (id, cfg) => bar(id, cfg as Parameters<typeof bar>[1]),
  box: (id, cfg) => box(id, cfg as Parameters<typeof box>[1]),
  parcoords: (id, cfg) => parcoords(id, cfg as Parameters<typeof parcoords>[1]),
  scatter3d: (id, cfg) => scatter3d(id, cfg as Parameters<typeof scatter3d>[1]),
  heatmap: (id, cfg) => heatmap(id, cfg as Parameters<typeof heatmap>[1]),
};

export function renderChart(cfg: ChartRenderConfig): void {
  const renderer = renderers[cfg.type];
  if (renderer) renderer(cfg.containerId, cfg.config);
}
