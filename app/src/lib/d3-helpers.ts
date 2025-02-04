import * as d3 from "d3";

/**
 * Initializes the Chart.js line chart.
 */
export function initD3Chart(chartContainer: HTMLElement) {
  // Clear existing chart
  d3.select(chartContainer).selectAll("*").remove();

  // Set margins
  const margin = { top: 40, right: 40, bottom: 60, left: 60 };
  const width = chartContainer.clientWidth;
  const height = chartContainer.clientHeight;
  const innerWidth = width - margin.left - margin.right;
  const innerHeight = height - margin.top - margin.bottom;

  // Create SVG with viewBox for responsiveness
  const svg = d3
    .select(chartContainer)
    .append("svg")
    .attr("viewBox", `0 0 ${width} ${height}`)
    .attr("width", "100%")
    .attr("height", "100%");

  // Create main group element with margin transform
  const g = svg
    .append("g")
    .attr("transform", `translate(${margin.left},${margin.top})`);

  // Create scales
  const xScale = d3.scaleLinear().range([0, innerWidth]);
  const yScale = d3.scaleLinear().range([innerHeight, 0]);

  // Create line generator with smoother curve
  const line = d3
    .line<number>()
    .x((d: number, i: number): number => xScale(i))
    .y((d: number): number => yScale(d))
    .curve(d3.curveCatmullRom.alpha(0.5));

  // Add X axis with styled ticks
  const xAxis = g
    .append("g")
    .attr("class", "x-axis")
    .attr("transform", `translate(0,${innerHeight})`)
    .attr("color", "#94a3b8");

  // Add Y axis with styled ticks
  const yAxis = g
    .append("g")
    .attr("class", "y-axis")
    .attr("color", "#94a3b8");

  // Add styled grid lines
  g.append("g")
    .attr("class", "grid")
    .attr("color", "#334155")
    .call(d3.axisLeft(yScale)
      .tickSize(-innerWidth)
      .tickFormat(() => "")
    );

  g.append("text")
    .attr("class", "x-label")
    .attr("text-anchor", "middle")
    .attr("x", innerWidth / 2)
    .attr("y", innerHeight + 50)
    .attr("fill", "#e2e8f0")
    .text("Individual");

  g.append("text")
    .attr("class", "y-label")
    .attr("text-anchor", "middle")
    .attr("fill", "#e2e8f0")
    .attr("transform", "rotate(-90)")
    .attr("x", -innerHeight / 2)
    .attr("y", -40)
    .text("Fitness");

  const path = g
    .append("path")
    .attr("class", "line")
    .attr("fill", "none")
    .attr("stroke", "#60a5fa")
    .attr("stroke-width", 2.5)
    .style("filter", "drop-shadow(0 0 6px rgba(96, 165, 250, 0.1))");

  return { xScale, yScale, line, path, xAxis, yAxis };
}

/**
 * 
 * @param chartContainer 
 * @param fitnessValues 
 */
export function updateFitnessChart(chartContainer: HTMLDivElement, fitnessValues: number[]) {
  const { xScale, yScale, line, path, xAxis, yAxis } =
    initD3Chart(chartContainer);
  xScale.domain([0, fitnessValues.length - 1]);
  const yMin = d3.min(fitnessValues) || 0;
  const yMax = d3.max(fitnessValues) || 1;
  const yPadding = (yMax - yMin) * 0.1;
  yScale.domain([yMin - yPadding, yMax + yPadding]);
  xAxis.call(d3.axisBottom(xScale));
  yAxis.call(d3.axisLeft(yScale));
  path.datum(fitnessValues).attr("d", line);
}