import type { ParcoordsConfig } from "../types";
import {
  getPlotly,
  buildLayout,
  defaultPlotlyConfig,
  isValidNumericArray,
  logChartError,
  getResponsiveMargins,
} from "../core";

export function parcoords(containerId: string, cfg: ParcoordsConfig): void {
  try {
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

    if (cfg.color && !isValidNumericArray(cfg.color)) {
      throw new Error("color must be a numeric array");
    }
    if (cfg.color && cfg.color.length !== dataLength) {
      throw new Error("color array length must match dimension value length");
    }

    const P = getPlotly();

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

    // ── Filtered individual list ──────────────────────────────────────────────
    if (cfg.filterList && cfg.hoverLabels && cfg.hoverLabels.length > 0) {
      const labels = cfg.hoverLabels;
      const dimValues = cfg.dimensions.map((d) => d.values);
      const n = labels.length;

      const wrapper = document.createElement("div");
      wrapper.className = "pc-filter-wrapper";

      const header = document.createElement("div");
      header.className = "pc-filter-header";

      const countEl = document.createElement("span");
      countEl.className = "pc-filter-count";
      countEl.textContent = `${n} of ${n} individuals`;

      const clearBtn = document.createElement("button");
      clearBtn.className = "pc-filter-clear";
      clearBtn.textContent = "Clear filters";
      clearBtn.style.display = "none";

      header.appendChild(countEl);
      header.appendChild(clearBtn);

      const listEl = document.createElement("div");
      listEl.className = "pc-filter-list";

      wrapper.appendChild(header);
      wrapper.appendChild(listEl);

      const el = document.getElementById(containerId);
      if (el && el.parentElement) {
        el.parentElement.insertBefore(wrapper, el.nextSibling);
      }

      const renderList = (indices: number[]) => {
        listEl.innerHTML = "";
        if (indices.length === 0) {
          const empty = document.createElement("div");
          empty.className = "pc-filter-empty";
          empty.textContent = "No individuals match the current filters.";
          listEl.appendChild(empty);
          return;
        }
        const frag = document.createDocumentFragment();
        for (const i of indices) {
          const row = document.createElement("div");
          row.className = "pc-filter-row";
          row.textContent = labels[i];
          frag.appendChild(row);
        }
        listEl.appendChild(frag);
      };

      const computeActive = (constraints: ([number, number] | null)[]): number[] => {
        const active: number[] = [];
        outer: for (let i = 0; i < n; i++) {
          for (let d = 0; d < dimValues.length; d++) {
            const c = constraints[d];
            if (!c) continue;
            const v = dimValues[d][i];
            if (v < c[0] || v > c[1]) continue outer;
          }
          active.push(i);
        }
        return active;
      };

      let activeConstraints: ([number, number] | null)[] = dimValues.map(() => null);

      renderList(Array.from({ length: n }, (_, i) => i));

      // Plotly fires plotly_restyle after every brush interaction.
      // update[0] is an object like { "dimensions[0].constraintrange": [[lo,hi]], ... }
      // or for a full trace update { "dimensions": [<full dims array>] }.
      // We re-read constraintrange directly from the live trace to avoid format guessing.
      (el as any)?.on("plotly_restyle", () => {
        const plotEl = document.getElementById(containerId) as any;
        if (!plotEl?.data?.[0]?.dimensions) return;

        const liveDims: any[] = plotEl.data[0].dimensions;
        let hasAnyFilter = false;

        for (let d = 0; d < dimValues.length; d++) {
          const cr = liveDims[d]?.constraintrange;
          if (cr != null) {
            // constraintrange is [lo, hi] for a single brush
            // or [[lo1,hi1],[lo2,hi2],...] for multiple brushes — use the first
            const first = Array.isArray(cr[0]) ? cr[0] : cr;
            activeConstraints[d] = [first[0] as number, first[1] as number];
            hasAnyFilter = true;
          } else {
            activeConstraints[d] = null;
          }
        }

        clearBtn.style.display = hasAnyFilter ? "" : "none";
        const active = computeActive(activeConstraints);
        countEl.textContent = `${active.length} of ${n} individuals`;
        renderList(active);
      });

      clearBtn.addEventListener("click", () => {
        const cleanDims = dimensions.map((d: any) => ({ ...d, constraintrange: undefined }));
        (P as any).restyle(containerId, { dimensions: [cleanDims] }, [0]);
        activeConstraints = dimValues.map(() => null);
        clearBtn.style.display = "none";
        countEl.textContent = `${n} of ${n} individuals`;
        renderList(Array.from({ length: n }, (_, i) => i));
      });
    }
  } catch (error) {
    logChartError("parcoords", error);
    renderErrorState(containerId, error);
  }
}

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
