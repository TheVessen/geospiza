/**
 * Common options shared by all chart types.
 * @property title - Chart title displayed above the plot
 * @property xAxis - X-axis label
 * @property yAxis - Y-axis label
 * @property height - Chart height in pixels (default: auto)
 * @property layout - Plotly layout overrides, merged with defaults
 * @property config - Plotly config overrides
 * @property containerId - Chart container element ID (for internal use)
 */
export interface ChartOptions {
  title?: string;
  xAxis?: string;
  yAxis?: string;
  height?: number;
  layout?: Record<string, unknown>;
  config?: Record<string, unknown>;
  containerId?: string;
}

/**
 * Scatter plot configuration.
 * @property x - Array of x-axis values (required)
 * @property y - Array of y-axis values (required, must match x length)
 * @property color - Optional numeric array for color scaling (must match x length)
 * @property colorScale - Plotly color scale name (default: "Viridis")
 * @property colorBarTitle - Title for color bar
 * @property text - Optional hover text for each point (must match x length)
 * @property size - Marker size in pixels (default: 4)
 * @property mode - Marker mode: "markers", "lines", or "lines+markers" (default: "markers")
 * @property useWebGL - Use WebGL rendering for better performance with large datasets
 * @property opacity - Marker opacity 0-1 (default: 0.7)
 * @property customdata - Custom data for advanced hover templates
 */
export interface ScatterConfig extends ChartOptions {
  x: number[];
  y: number[];
  color?: number[];
  colorScale?: string;
  colorBarTitle?: string;
  text?: string[];
  size?: number | number[];
  mode?: "markers" | "lines" | "lines+markers";
  useWebGL?: boolean;
  opacity?: number;
  customdata?: unknown[];
}

/**
 * Line trace data.
 * @property x - X-axis values
 * @property y - Y-axis values (must match x length)
 * @property name - Trace name for legend
 * @property dash - Line dash style: "solid", "dot", "dash", "longdash", etc.
 * @property color - Line color
 * @property width - Line width in pixels
 */
export interface LineTrace {
  x: number[];
  y: number[];
  name: string;
  dash?: string;
  color?: string;
  width?: number;
}

/**
 * Fill band for line charts (e.g., confidence intervals, standard deviation).
 * @property upper - Upper boundary values
 * @property lower - Lower boundary values
 * @property x - X-axis values (must match upper/lower lengths)
 * @property color - Fill color with alpha
 */
export interface FillBand {
  upper: number[];
  lower: number[];
  x: number[];
  color?: string;
}

/**
 * Line chart configuration.
 * @property traces - Array of line traces (required, at least one)
 * @property fill - Optional fill band for shaded areas
 * @property hoverXLabel - Short prefix for the x-value in the hover tooltip (e.g. "G" → "G=5")
 * @property hoverYLabel - Short prefix for the y-value in the hover tooltip (e.g. "F" → "F=1")
 */
export interface LineConfig extends ChartOptions {
  traces: LineTrace[];
  fill?: FillBand;
  hoverXLabel?: string;
  hoverYLabel?: string;
}

/**
 * Bar chart configuration.
 * @property categories - Category labels (required)
 * @property values - Bar heights (required, must match categories length)
 * @property orientation - "v" for vertical, "h" for horizontal (default: "v")
 * @property colors - Optional array of colors per bar (must match values length)
 */
export interface BarConfig extends ChartOptions {
  categories: string[];
  values: number[];
  orientation?: "v" | "h";
  colors?: string[];
}

/**
 * Box plot configuration (statistical distribution).
 * @property groups - Array of data groups with names
 */
export interface BoxConfig extends ChartOptions {
  groups: { data: number[]; name: string }[];
}

/**
 * Parallel coordinates configuration (multivariate analysis).
 * @property dimensions - Array of dimensions with values and optional range bounds
 * @property color - Optional numeric array for line coloring
 * @property colorScale - Plotly color scale name
 * @property colorBarTitle - Title for color bar
 */
export interface ParcoordsConfig extends ChartOptions {
  dimensions: { label: string; values: number[]; range?: [number, number] }[];
  color?: number[];
  colorScale?: string;
  colorBarTitle?: string;
  /** Reverse the colorscale direction (e.g. so high values are red, low values are blue). */
  reverseScale?: boolean;
  /**
   * Per-individual labels used as row text in the filter list.
   * Typically the same hover string used on scatter plots
   * (e.g. "Gen: 5 | #Ind 12/100 | Fitness: 0.1234").
   */
  hoverLabels?: string[];
  /**
   * When true, renders a scrollable filtered list below the chart that shows
   * only the individuals whose values satisfy all active axis constraints.
   * Requires hoverLabels to be set.
   */
  filterList?: boolean;
}

/**
 * 3D scatter plot configuration.
 * @property x - X-axis values (required)
 * @property y - Y-axis values (required, must match x length)
 * @property z - Z-axis values (required, must match x length)
 * @property zAxis - Z-axis label
 * @property color - Optional numeric array for color scaling (must match x length)
 * @property colorScale - Plotly color scale name
 * @property colorBarTitle - Title for color bar
 * @property size - Marker size (default: 4)
 * @property text - Optional hover text (must match x length)
 */
export interface Scatter3dConfig extends ChartOptions {
  x: number[];
  y: number[];
  z: number[];
  zAxis?: string;
  color?: number[];
  colorScale?: string;
  colorBarTitle?: string;
  size?: number;
  text?: string[];
}

/**
 * Heatmap configuration.
 * @property z - 2D numeric array (required)
 * @property x - Column labels (optional)
 * @property y - Row labels (optional)
 * @property colorScale - Plotly color scale name
 * @property colorBarTitle - Title for color bar
 */
export interface HeatmapConfig extends ChartOptions {
  z: number[][];
  x?: string[];
  y?: string[];
  colorScale?: string;
  colorBarTitle?: string;
}

/**
 * Chart render config passed from C# to HTML/JS.
 * @property type - Chart type
 * @property containerId - DOM element ID
 * @property config - Chart-specific configuration
 */
export interface ChartRenderConfig {
  type: string;
  containerId: string;
  config: Record<string, unknown>;
}

/**
 * Dashboard configuration embedded in HTML.
 * @property tabs - Array of tab definitions
 */
export interface DashboardConfig {
  tabs: { name: string; charts: ChartRenderConfig[] }[];
}
