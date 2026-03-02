import type { ChartOptions, Plotly } from "./types";

declare const window: { Plotly: typeof Plotly };

/** Get computed CSS variable value. */
export function getCSSVariable(varName: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
}

/** Get current theme mode (light or dark). */
export function isDarkMode(): boolean {
  return !document.documentElement.classList.contains("light");
}

/** Validate numeric array. */
export function isValidNumericArray(arr: unknown): arr is number[] {
  return (
    Array.isArray(arr) &&
    arr.length > 0 &&
    arr.every((v) => typeof v === "number" && isFinite(v))
  );
}

/** Validate string array. */
export function isValidStringArray(arr: unknown): arr is string[] {
  return (
    Array.isArray(arr) &&
    arr.length > 0 &&
    arr.every((v) => typeof v === "string")
  );
}

/** Check if arrays have matching lengths. */
export function arraysMatch(...arrays: unknown[][]): boolean {
  if (arrays.length === 0) return false;
  const len = arrays[0].length;
  return arrays.every((a) => Array.isArray(a) && a.length === len);
}

/** Log chart error safely. */
export function logChartError(chartType: string, error: unknown): void {
  console.error(`[ChartHopper] ${chartType} error:`, error);
}

/** Default Plotly config shared across all charts. */
export function defaultPlotlyConfig(
  opts?: ChartOptions
): Record<string, unknown> {
  return {
    responsive: true,
    displayModeBar: true,
    modeBarButtonsToRemove: ["lasso2d", "select2d"],
    toImageButtonOptions: {
      format: "svg",
      filename: "chart",
    },
    ...opts?.config,
  };
}

/** Calculate responsive margins based on container size. */
export function getResponsiveMargins(
  containerId: string,
  defaults = { t: 40, b: 50, l: 60, r: 20 }
): { t: number; b: number; l: number; r: number } {
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

/** Build base layout from common options, merged with user overrides. */
export function buildLayout(opts?: ChartOptions): Record<string, unknown> {
  const base: Record<string, unknown> = {
    margin: getResponsiveMargins(opts?.containerId || ""),
    font: { color: getCSSVariable("--text-primary") },
    paper_bgcolor: "transparent",
    plot_bgcolor: getCSSVariable("--plot-bg"),
    xaxis: { gridcolor: getCSSVariable("--grid-color"), zerolinecolor: getCSSVariable("--grid-zero") },
    yaxis: { gridcolor: getCSSVariable("--grid-color"), zerolinecolor: getCSSVariable("--grid-zero") },
  };
  if (opts?.title) base.title = opts.title;
  if (opts?.xAxis) base.xaxis = { ...((base.xaxis as object) ?? {}), title: opts.xAxis };
  if (opts?.yAxis) base.yaxis = { ...((base.yaxis as object) ?? {}), title: opts.yAxis };
  if (opts?.height) base.height = opts.height;

  return { ...base, ...opts?.layout };
}

/** Get the Plotly global. */
export function getPlotly(): typeof Plotly {
  return window.Plotly;
}
