using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ChartHopper;
using GeospizaCore.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Analysis;

public class GH_AnalysisExport : GH_Component
{
    public GH_AnalysisExport()
        : base("Analysis Export", "Export",
            "Exports evolution data as an interactive analysis dashboard in the browser",
            "Geospiza", "Analysis")
    {
    }

    protected override Bitmap Icon => null;

    public override Guid ComponentGuid => new("A7E2D1F4-3B8C-4A6E-9D5F-1C2E8B4A7F30");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The EvolutionObserver with completed run data",
            GH_ParamAccess.item);
        pManager.AddTextParameter("Directory", "D", "Output directory (defaults to Desktop)",
            GH_ParamAccess.item);
        pManager.AddTextParameter("FileName", "F", "Base filename without extension",
            GH_ParamAccess.item);
        pManager.AddBooleanParameter("Open", "Op", "Open the viewer in the default browser after export",
            GH_ParamAccess.item, true);
        pManager.AddBooleanParameter("Export", "E", "Save HTML to disk and optionally open in browser",
            GH_ParamAccess.item, false);
        pManager.AddBooleanParameter("Preview", "P",
            "Open a temporary browser preview without saving to disk — toggle to refresh",
            GH_ParamAccess.item, false);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("FilePath", "FP", "Path to the exported HTML file", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var export = false;
        var preview = false;
        DA.GetData(4, ref export);
        DA.GetData(5, ref preview);

        if (!export && !preview) return;

        GH_ObjectWrapper observerWrapper = null;
        if (!DA.GetData(0, ref observerWrapper)) return;

        if (observerWrapper.ScriptVariable() is not EvolutionObserver observer)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not an EvolutionObserver");
            return;
        }

        if (observer.CurrentGenerationIndex == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Observer has no data yet");
            return;
        }

        try
        {
            var html = BuildEvolutionDashboard(observer);

            // Preview: write to OS temp dir and open — no permanent file saved
            if (preview)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"geospiza-preview-{Guid.NewGuid():N}.html");
                File.WriteAllText(tempPath, html);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
            }

            // Export: save to specified directory
            if (export)
            {
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var tempDir = "";
                if (DA.GetData(1, ref tempDir) && !string.IsNullOrWhiteSpace(tempDir))
                    directory = tempDir;

                var fileName = $"evolution-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                var tempName = "";
                if (DA.GetData(2, ref tempName) && !string.IsNullOrWhiteSpace(tempName))
                    fileName = tempName;

                var openInBrowser = true;
                DA.GetData(3, ref openInBrowser);

                var filePath = HtmlExporter.Export(html, directory, fileName, openInBrowser);
                DA.SetData(0, filePath);
            }
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Dashboard failed: {ex.Message}");
        }
    }

    private static string BuildEvolutionDashboard(EvolutionObserver obs)
    {
        var genCount = obs.CurrentGenerationIndex;
        var gens = Enumerable.Range(0, genCount).ToArray();
        var gensD = gens.Select(i => (double)i).ToArray();
        var isMultiObjective = obs.ObjectiveNames is { Length: > 0 };

        var db = new DashboardBuilder().Title("Geospiza Evolution Analysis");

        // ── Tab 1: Fitness Evolution ───────────────────────────────────────────
        var fitnessChart = new ChartBuilder(ChartType.Line)
            .Title("Fitness Progression")
            .AddTrace(gens, obs.BestFitness, "Best", color: "#2ecc71")
            .AddTrace(gens, obs.AverageFitness, "Average", color: "#3498db")
            .AddTrace(gens, obs.WorstFitness, "Worst", color: "#e74c3c", dash: "dot")
            .XAxis("Generation").YAxis("Fitness").Height(400)
            .LineHoverLabels("G", "F");

        // Add std-dev band around average
        if (obs.FitnessStandardDeviation.Count == genCount)
        {
            var upper = new double[genCount];
            var lower = new double[genCount];
            for (var i = 0; i < genCount; i++)
            {
                upper[i] = obs.AverageFitness[i] + obs.FitnessStandardDeviation[i];
                lower[i] = obs.AverageFitness[i] - obs.FitnessStandardDeviation[i];
            }
            fitnessChart.FillBand(gensD, upper, lower, "rgba(52,152,219,0.10)");
        }

        var diversityChart = new ChartBuilder(ChartType.Line)
            .Title("Population Diversity — Unique Individuals")
            .AddTrace(gens, obs.NumberOfUniqueIndividuals, "Unique Individuals", color: "#9b59b6")
            .XAxis("Generation").YAxis("Unique Individuals").Height(300);

        var convergenceChart = new ChartBuilder(ChartType.Line)
            .Title("Convergence — Fitness Std Deviation")
            .AddTrace(gens, obs.FitnessStandardDeviation, "Std Dev", color: "#e67e22")
            .XAxis("Generation").YAxis("Std Deviation").Height(300);

        var boxChart = new ChartBuilder(ChartType.Box)
            .Title("Fitness Distribution per Generation (sampled)")
            .XAxis("Generation").YAxis("Fitness").Height(400);
        var boxStep = genCount > 50 ? (int)Math.Ceiling(genCount / 50.0) : 1;
        for (var g = 0; g < genCount; g += boxStep)
            if (g < obs.AllGenerations.Count)
                boxChart.AddGroup(g.ToString(), obs.AllGenerations[g].Select(s => s.Fitness).ToArray());

        db.AddTab("Fitness Evolution", fitnessChart, diversityChart, convergenceChart, boxChart);

        // ── Tab 2: Parameter Influence ────────────────────────────────────────
        if (obs.GeneSchema is { Length: > 0 } && obs.AllGenerations.Count > 0)
        {
            var paramCharts = new List<ChartBuilder>();
            // Track each individual's index within its generation so hover labels can
            // identify the exact individual (e.g. "Gen 5 | #Ind 12/100").
            var allTagged = obs.AllGenerations
                .SelectMany((g, gi) => g.Select((s, si) => (snap: s, gi, si, total: g.Count)))
                .ToArray();
            var allSnaps = allTagged.Select(t => t.snap).ToArray();
            var allFitness = allTagged.Select(t => t.snap.Fitness).ToArray();
            var allGens = allTagged.Select(t => (double)t.snap.Generation).ToArray();
            var allHover = allTagged
                .Select(t => $"Gen: {t.gi} | #Ind {t.si}/{t.total} | Fitness: {t.snap.Fitness:F4}")
                .ToArray();

            // Per-gene scatter vs fitness, colored by generation, with hover labels
            foreach (var gene in obs.GeneSchema)
            {
                var idx = gene.GenePoolIndex;
                var tickCount = gene.TickCount;
                if (idx < 0 || allSnaps.Any(s => s.TickValues == null || idx >= s.TickValues.Length))
                    continue;

                var geneVals = allSnaps
                    .Select(s => tickCount > 1 ? (double)s.TickValues[idx] / (tickCount - 1) : 0.0)
                    .ToArray();

                paramCharts.Add(new ChartBuilder(ChartType.Scatter)
                    .Title($"{gene.GeneName} vs Fitness")
                    .Data(geneVals, allFitness)
                    .Color(allGens, colorBarTitle: "Generation")
                    .Text(allHover)
                    .XAxis($"{gene.GeneName} (0–1 normalised)").YAxis("Fitness")
                    .UseWebGL().Opacity(0.5).MarkerSize(3).Height(350));
            }

            // Spearman correlation bar chart — positive = higher value → better fitness
            var geneNames = new List<string>();
            var correlations = new List<double>();
            var barColors = new List<string>();
            foreach (var gene in obs.GeneSchema)
            {
                var idx = gene.GenePoolIndex;
                var tickCount = gene.TickCount;
                if (idx < 0 || allSnaps.Any(s => s.TickValues == null || idx >= s.TickValues.Length))
                    continue;

                var geneVals = allSnaps
                    .Select(s => tickCount > 1 ? (double)s.TickValues[idx] / (tickCount - 1) : 0.0)
                    .ToArray();
                var corr = SpearmanCorrelation(geneVals, allFitness);
                geneNames.Add(gene.GeneName);
                correlations.Add(corr);
                barColors.Add(corr >= 0 ? "rgba(55,128,191,0.85)" : "rgba(219,64,82,0.85)");
            }

            if (geneNames.Count > 0)
                paramCharts.Add(new ChartBuilder(ChartType.Bar)
                    .Title("Parameter–Fitness Correlation (Spearman ρ)")
                    .Categories(geneNames, correlations)
                    .BarColors(barColors)
                    .Horizontal()
                    .Height(Math.Max(300, geneNames.Count * 35 + 80)));

            // Parallel coordinates of parameter space — final generation with fitness as last axis.
            // Drag axes to filter: e.g. brush high-fitness region to see which parameters produced it.
            // Each line is one individual — use the Highlight panel to isolate a specific individual.
            var lastGenParam = obs.AllGenerations[obs.AllGenerations.Count - 1];
            // Build per-individual hover labels for the highlight panel info bar
            var lastGenParamHover = lastGenParam
                .Select((s, si) => $"#Ind {si}/{lastGenParam.Count} | Fitness: {s.Fitness:F4}")
                .ToArray();
            var pcParam = new ChartBuilder(ChartType.Parcoords)
                .Title($"Parameter Space — Parallel Coordinates (Generation {genCount - 1})")
                .Height(500);
            foreach (var gene in obs.GeneSchema)
            {
                var idx = gene.GenePoolIndex;
                var tickCount = gene.TickCount;
                if (idx < 0 || lastGenParam.Any(s => s.TickValues == null || idx >= s.TickValues.Length))
                    continue;
                var dimVals = lastGenParam
                    .Select(s => tickCount > 1 ? (double)s.TickValues[idx] / (tickCount - 1) : 0.0)
                    .ToArray();
                pcParam.AddDimension(gene.GeneName, dimVals, 0, 1);
            }
            var lastGenFitness = lastGenParam.Select(s => s.Fitness).ToArray();
            if (lastGenFitness.Length > 0)
            {
                pcParam.AddDimension("Fitness", lastGenFitness);
                // RdYlBu reversed: blue=low fitness, green=mid, red=high — same visual logic as Wallacei.
                // EnableHighlight injects a control bar so users can isolate any individual by index.
                pcParam.Color(lastGenFitness, "RdYlBu", "Fitness")
                       .ReverseScale()
                       .EnableHighlight(lastGenParamHover);
            }
            paramCharts.Add(pcParam);

            db.AddTab("Parameter Influence", paramCharts.ToArray());
        }

        // ── Tab 3: Individual Explorer ────────────────────────────────────────
        if (obs.AllGenerations.Count > 0)
        {
            // Tag each snapshot with its generation index and position within that generation
            // so the hover tooltip can uniquely identify every individual.
            var explorerTagged = obs.AllGenerations
                .SelectMany((g, gi) => g.Select((s, si) => (snap: s, gi, si, total: g.Count)))
                .ToArray();
            var allSnaps = explorerTagged.Select(t => t.snap).ToArray();

            // Hover shows generation, individual index, fitness, and for MO: rank + objectives
            string[] hoverLabels;
            if (isMultiObjective)
            {
                var objNames = obs.ObjectiveNames!;
                hoverLabels = explorerTagged.Select(t =>
                {
                    var s = t.snap;
                    var objStr = s.Objectives != null
                        ? string.Join(" | ", s.Objectives.Select((o, i) =>
                            $"{(i < objNames.Length ? objNames[i] : $"Obj{i}")}: {o:F4}"))
                        : "";
                    return $"Gen: {t.gi} | #Ind {t.si}/{t.total} | Fit: {s.Fitness:F4} | Rank: {s.ParetoRank} | {objStr}";
                }).ToArray();
            }
            else
            {
                hoverLabels = explorerTagged
                    .Select(t => $"Gen: {t.gi} | #Ind {t.si}/{t.total} | Fit: {t.snap.Fitness:F4}")
                    .ToArray();
            }

            var explorerChart = new ChartBuilder(ChartType.Scatter)
                .Title("All Individuals — Fitness over Generations")
                .Data(allSnaps.Select(s => (double)s.Generation), allSnaps.Select(s => s.Fitness))
                .Color(allSnaps.Select(s => (double)s.Generation), colorBarTitle: "Generation")
                .Text(hoverLabels)
                .XAxis("Generation").YAxis("Fitness")
                .UseWebGL().Opacity(0.5).MarkerSize(3).Height(450);

            var bestTrajectory = new ChartBuilder(ChartType.Line)
                .Title("Best Individual Trajectory")
                .AddTrace(gens, obs.BestFitness, "Best Fitness", color: "#2ecc71", width: 2)
                .AddTrace(gens, obs.AverageFitness, "Average Fitness", color: "#3498db", dash: "dot")
                .XAxis("Generation").YAxis("Fitness").Height(300);

            db.AddTab("Individual Explorer", explorerChart, bestTrajectory);
        }

        // ── Tab 4: Pareto Front (multi-objective only) ────────────────────────
        if (isMultiObjective && obs.AllGenerations.Count > 0)
        {
            var paretoCharts = new List<ChartBuilder>();
            var objNames = obs.ObjectiveNames!;

            var lastGen = obs.AllGenerations[obs.AllGenerations.Count - 1];
            // Keep individual index so hover labels can identify the exact individual
            var lastGenTagged = lastGen
                .Select((s, si) => (snap: s, si, total: lastGen.Count))
                .Where(t => t.snap.Objectives != null && t.snap.Objectives.Length >= objNames.Length)
                .ToArray();
            var withObj = lastGenTagged.Select(t => t.snap).ToArray();

            if (withObj.Length > 0)
            {
                // Rich hover: individual index + rank + crowding + all objective values
                var paretoHover = lastGenTagged.Select(t =>
                {
                    var s = t.snap;
                    var objStr = string.Join(" | ", s.Objectives!.Select((o, i) =>
                        $"{(i < objNames.Length ? objNames[i] : $"Obj{i}")}: {o:F4}"));
                    return $"#Ind {t.si}/{t.total} | Rank: {s.ParetoRank} | Crowding: {s.CrowdingDistance:F3} | {objStr}";
                }).ToArray();

                // 2-objective Pareto scatter
                if (objNames.Length == 2)
                {
                    paretoCharts.Add(new ChartBuilder(ChartType.Scatter)
                        .Title($"Pareto Front (Gen {genCount - 1})")
                        .Data(withObj.Select(s => s.Objectives![0]), withObj.Select(s => s.Objectives![1]))
                        .Color(withObj.Select(s => (double)s.ParetoRank), colorBarTitle: "Pareto Rank")
                        .Text(paretoHover)
                        .XAxis(objNames[0]).YAxis(objNames[1])
                        .MarkerSize(6).Height(500));
                }
                // 3+-objective Pareto scatter (3D)
                else if (objNames.Length >= 3)
                {
                    var withObj3d = withObj.Where(s => s.Objectives!.Length >= 3).ToArray();
                    if (withObj3d.Length > 0)
                        paretoCharts.Add(new ChartBuilder(ChartType.Scatter3d)
                            .Title($"Pareto Front 3D (Gen {genCount - 1})")
                            .Data(withObj3d.Select(s => s.Objectives![0]), withObj3d.Select(s => s.Objectives![1]))
                            .ZData(withObj3d.Select(s => s.Objectives![2]))
                            .Color(withObj3d.Select(s => (double)s.ParetoRank), colorBarTitle: "Pareto Rank")
                            .XAxis(objNames[0]).YAxis(objNames[1]).ZAxis(objNames[2])
                            .Height(600));
                }

                // ── Parcoords: final-gen trade-offs across ALL objectives ────
                // Every line = one individual. Axes = objectives + crowding distance.
                // Brush an axis range to isolate solutions that satisfy specific bounds on any objective.
                // Color = Pareto rank (rank 0 = non-dominated front).
                var pcFinalObj = new ChartBuilder(ChartType.Parcoords)
                    .Title($"Objective Trade-offs — Parallel Coordinates (Gen {genCount - 1})")
                    .Height(500);
                for (var m = 0; m < objNames.Length; m++)
                {
                    var mi = m;
                    pcFinalObj.AddDimension(objNames[m], withObj.Select(s => s.Objectives![mi]).ToArray());
                }
                // Cap infinite crowding distances (boundary individuals) to keep axis usable
                var crowding = withObj
                    .Select(s => double.IsInfinity(s.CrowdingDistance) ? 1e4 : s.CrowdingDistance)
                    .ToArray();
                pcFinalObj.AddDimension("Crowding Dist", crowding);
                pcFinalObj.Color(withObj.Select(s => (double)s.ParetoRank), colorBarTitle: "Pareto Rank");
                paretoCharts.Add(pcFinalObj);

                // ── Parcoords: all-generation objective evolution ────────────
                // Colored by generation — lets you see visually how the population
                // migrated / converged through objective space across all generations.
                var allWithObj = obs.AllGenerations
                    .SelectMany(gg => gg)
                    .Where(s => s.Objectives != null && s.Objectives.Length >= objNames.Length)
                    .ToArray();

                if (allWithObj.Length > 0)
                {
                    // Subsample to keep rendering fast (max 5 000 lines)
                    var sampleStep = allWithObj.Length > 5000
                        ? (int)Math.Ceiling(allWithObj.Length / 5000.0) : 1;
                    var sampled = allWithObj.Where((_, i) => i % sampleStep == 0).ToArray();

                    var pcAllObj = new ChartBuilder(ChartType.Parcoords)
                        .Title("All Generations — Objective Space Evolution")
                        .Height(500);
                    for (var m = 0; m < objNames.Length; m++)
                    {
                        var mi = m;
                        pcAllObj.AddDimension(objNames[m], sampled.Select(s => s.Objectives![mi]).ToArray());
                    }
                    pcAllObj.AddDimension("Generation", sampled.Select(s => (double)s.Generation).ToArray());
                    pcAllObj.Color(sampled.Select(s => (double)s.Generation), colorBarTitle: "Generation");
                    paretoCharts.Add(pcAllObj);
                }

                // ── Pareto front size evolution ──────────────────────────────
                if (obs.ParetoFrontSizes.Count == genCount)
                    paretoCharts.Add(new ChartBuilder(ChartType.Line)
                        .Title("Rank-0 Front Size per Generation")
                        .AddTrace(gensD, obs.ParetoFrontSizes.Select(v => (double)v), "Rank-0 Count",
                            color: "#1abc9c")
                        .XAxis("Generation").YAxis("# Rank-0 Individuals").Height(300));

                // ── Hypervolume progression ──────────────────────────────────
                if (obs.Hypervolume.Count == genCount)
                    paretoCharts.Add(new ChartBuilder(ChartType.Line)
                        .Title("Hypervolume Indicator")
                        .AddTrace(gens, obs.Hypervolume, "Hypervolume", color: "#9b59b6")
                        .XAxis("Generation").YAxis("Hypervolume").Height(350));

                // ── Per-objective progressions: min / mean / max with fill band ──
                if (obs.ObjectiveStats.Count == genCount)
                {
                    for (var m = 0; m < objNames.Length; m++)
                    {
                        var mi = m;
                        var mins = gensD.Select(g => GetObjStat(obs, (int)g, mi, 0)).ToArray();
                        var maxs = gensD.Select(g => GetObjStat(obs, (int)g, mi, 1)).ToArray();
                        var means = gensD.Select(g => GetObjStat(obs, (int)g, mi, 2)).ToArray();

                        paretoCharts.Add(new ChartBuilder(ChartType.Line)
                            .Title($"{objNames[m]} — Progression (min / mean / max)")
                            .AddTrace(gens, means, "Mean", color: "#3498db")
                            .AddTrace(gens, mins, "Min", color: "#2ecc71", dash: "dot")
                            .AddTrace(gens, maxs, "Max", color: "#e74c3c", dash: "dot")
                            .FillBand(gensD, maxs, mins, "rgba(52,152,219,0.10)")
                            .XAxis("Generation").YAxis(objNames[m]).Height(350));
                    }
                }
            }

            if (paretoCharts.Count > 0)
                db.AddTab("Pareto Front", paretoCharts.ToArray());
        }

        return db.Build();
    }

    /// <summary>Safe accessor for ObjectiveStats[gen][objIdx][statIdx] where statIdx: 0=min, 1=max, 2=mean.</summary>
    private static double GetObjStat(EvolutionObserver obs, int gen, int objIdx, int statIdx)
    {
        if (gen >= obs.ObjectiveStats.Count) return 0.0;
        var row = obs.ObjectiveStats[gen];
        if (row == null || objIdx >= row.Length) return 0.0;
        var cell = row[objIdx];
        return cell != null && statIdx < cell.Length ? cell[statIdx] : 0.0;
    }

    /// <summary>Computes Spearman rank correlation between two equal-length arrays.</summary>
    private static double SpearmanCorrelation(double[] x, double[] y)
    {
        var n = x.Length;
        if (n < 2) return 0;

        var rankX = ComputeRanks(x);
        var rankY = ComputeRanks(y);

        var sumD2 = 0.0;
        for (var i = 0; i < n; i++)
        {
            var d = rankX[i] - rankY[i];
            sumD2 += d * d;
        }

        return 1.0 - 6.0 * sumD2 / (n * ((long)n * n - 1));
    }

    private static double[] ComputeRanks(double[] arr)
    {
        var indexed = arr.Select((v, i) => (v, i)).OrderBy(p => p.v).ToArray();
        var ranks = new double[arr.Length];

        var i = 0;
        while (i < indexed.Length)
        {
            var j = i;
            while (j < indexed.Length && indexed[j].v == indexed[i].v) j++;
            var avgRank = (i + j + 1.0) / 2.0;
            for (var k = i; k < j; k++)
                ranks[indexed[k].i] = avgRank;
            i = j;
        }

        return ranks;
    }
}
