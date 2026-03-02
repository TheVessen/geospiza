import type { ParcoordsConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  logChartError,
  getResponsiveMargins,
} from "../core";

/**
 * Render parallel coordinates plot for multivariate analysis.
 * Validates dimensions and provides error handling.
 * Supports optional per-individual highlight panel (enableHighlight) and
 * colorscale reversal (reverseScale).
 */
export function parcoords(containerId: string, cfg: ParcoordsConfig): void {
  try {
    // Validate dimensions
    if (!cfg.dimensions || !Array.isArray(cfg.dimensions) || cfg.dimensions.length === 0) {
      throw new Error("dimensions must be a non-empty array");
    }

    let dataLength: number | undefined;
    for (let i = 0; i < cfg.dimensions.length; i++) {
      const d = cfg.dimensions[i];
      if (!d.label || typeof d.label !== "string") {
        throw new Error(`dimension ${i}: label must be a non-empty string`);
      }
      if (!isValidNumericArray(d.values)) {
        throw new Error(`dimension ${i}: values must be a non-empty numeric array`);
      }
      if (dataLength === undefined) {
        dataLength = d.values.length;
      } else if (d.values.length !== dataLength) {
        throw new Error(`dimension ${i}: all dimensions must have the same value length`);
      }
    }

    // Validate optional color
    if (cfg.color && !isValidNumericArray(cfg.color)) {
      throw new Error("color must be a numeric array");
    }
    if (cfg.color && cfg.color.length !== dataLength) {
      throw new Error("color array length must match dimension value length");
    }

    const P = getPlotly();

    // Build dimensions with efficient min/max calculation
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

    const trace: Record<string, unknown> = {
      type: "parcoords",
      dimensions,
      line: cfg.color
        ? {
          color: cfg.color,
          colorscale: cfg.colorScale ?? "Viridis",
          reversescale: cfg.reverseScale ?? false,
          showscale: true,
          colorbar: cfg.colorBarTitle ? { title: cfg.colorBarTitle } : undefined,
        }
        : {},
    };

    const margin = getResponsiveMargins(containerId, {
      t: cfg.title ? 120 : 80,
      b: 60,
      l: 80,
      r: 80,
    });

    const titleLayout = cfg.title
      ? { text: cfg.title, y: 0.99, yanchor: "top" as const, pad: { b: 10 } }
      : undefined;

    const layout = {
      ...buildLayout({ ...cfg, containerId, title: undefined }),
      title: titleLayout,
      height: cfg.height ?? 450,
      margin,
    };

    P.newPlot(containerId, [trace], layout, defaultPlotlyConfig(cfg));

    // ── Individual highlight panel ────────────────────────────────────────────
    // After the chart is rendered, inject a small control bar above the chart
    // container so the user can highlight a specific individual (line) by index.
    // All other lines fade to near-transparent; the selected line turns vivid orange.
    // This is similar to Wallacei's individual inspector.
    if (cfg.enableHighlight && cfg.color && cfg.color.length > 0) {
      const originalColor = [...cfg.color];
      const originalScale = cfg.colorScale ?? "Viridis";
      const originalReverse = cfg.reverseScale ?? false;

      // Vivid highlight colorscale: gray background lines, orange selected line
      const HIGHLIGHT_SCALE = [
        [0, "rgba(150,150,150,0.10)"],
        [1, "#ff6b35"],
      ];

      const panel = document.createElement("div");
      panel.style.cssText =
        "display:flex;align-items:center;gap:0.5rem;padding:0.4rem 0.75rem;" +
        "font-size:0.8rem;color:var(--text-secondary);flex-wrap:wrap;";

      const btnStyle =
        "padding:3px 10px;border-radius:4px;border:1px solid var(--border-color);" +
        "background:var(--bg-tertiary);color:var(--text-primary);cursor:pointer;" +
        "font-size:0.8rem;transition:border-color 0.15s;";

      const maxIdx = originalColor.length - 1;
      panel.innerHTML =
        `<span>Highlight individual #</span>` +
        `<input type="number" id="${containerId}-hi-idx" min="0" max="${maxIdx}" ` +
        ` style="width:70px;padding:2px 6px;border-radius:4px;border:1px solid var(--border-color);` +
        ` background:var(--bg-tertiary);color:var(--text-primary);font-size:0.8rem" />` +
        `<button id="${containerId}-hi-apply" style="${btnStyle}">Highlight</button>` +
        `<button id="${containerId}-hi-clear" style="${btnStyle}">Clear</button>` +
        `<span id="${containerId}-hi-label" style="color:var(--text-secondary);font-style:italic"></span>`;

      const el = document.getElementById(containerId);
      if (el && el.parentElement) {
        el.parentElement.insertBefore(panel, el);
      }

      const applyHighlight = () => {
        const input = document.getElementById(`${containerId}-hi-idx`) as HTMLInputElement | null;
        const idx = input ? parseInt(input.value, 10) : NaN;
        if (isNaN(idx) || idx < 0 || idx > maxIdx) return;

        // All lines = 0 (maps to ghost gray), selected = 1 (maps to orange)
        const newColor = originalColor.map((_, i) => (i === idx ? 1 : 0));
        (P as unknown as Record<string, Function>)["restyle"](
          containerId,
          { "line.color": [newColor], "line.colorscale": [HIGHLIGHT_SCALE], "line.reversescale": [false] },
          [0]
        );

        const labelEl = document.getElementById(`${containerId}-hi-label`);
        if (labelEl) {
          const info = cfg.hoverLabels?.[idx] ?? `Individual ${idx}`;
          labelEl.textContent = `→ ${info}`;
        }
      };

      const clearHighlight = () => {
        (P as unknown as Record<string, Function>)["restyle"](
          containerId,
          {
            "line.color": [originalColor],
            "line.colorscale": [originalScale],
            "line.reversescale": [originalReverse],
          },
          [0]
        );
        const labelEl = document.getElementById(`${containerId}-hi-label`);
        if (labelEl) labelEl.textContent = "";
      };

      document.getElementById(`${containerId}-hi-apply`)?.addEventListener("click", applyHighlight);
      document.getElementById(`${containerId}-hi-clear`)?.addEventListener("click", clearHighlight);
      document.getElementById(`${containerId}-hi-idx`)?.addEventListener("keydown", (e: Event) => {
        if ((e as KeyboardEvent).key === "Enter") applyHighlight();
      });
    }
  } catch (error) {
    logChartError("parcoords", error);
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
