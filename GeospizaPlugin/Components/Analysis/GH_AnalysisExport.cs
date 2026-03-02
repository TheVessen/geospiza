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

        // ── Tab 1: Fitness Evolution (single-obj) / Population Statistics (multi-obj) ──
        var popSize = obs.AllGenerations.Count > 0 ? obs.AllGenerations[0].Count : 1;
        var algorithmLabel = obs.Algorithm switch
        {
            EvolutionObserver.AlgorithmType.NsgaII => "NSGA-II",
            EvolutionObserver.AlgorithmType.NsgaIII => "NSGA-III",
            _ => null
        };

        // Diversity normalised to % of population — meaningful for both algorithm types.
        var diversityPct = obs.NumberOfUniqueIndividuals
            .Select(u => popSize > 1 ? u * 100.0 / popSize : 0.0).ToArray();

        var diversityChart = new ChartBuilder(ChartType.Line)
            .Title("Population Diversity — Unique Individuals")
            .AddTrace(gens, diversityPct, "% Unique", color: "#9b59b6")
            .XAxis("Generation").YAxis("Unique Individuals (%)").Height(300)
            .LineHoverLabels("Gen", "Unique");

        var convergenceChart = new ChartBuilder(ChartType.Line)
            .Title("Convergence — Fitness Standard Deviation")
            .AddTrace(gens, obs.FitnessStandardDeviation, "Std Dev", color: "#e67e22")
            .XAxis("Generation").YAxis("Std Deviation").Height(300)
            .LineHoverLabels("Gen", "StdDev");

        if (algorithmLabel == null)
        {
            // ── Single-objective: full Fitness Evolution tab ──────────────────
            db.AddTab("Fitness Evolution");

            var fitnessChart = new ChartBuilder(ChartType.Line)
                .Title("Fitness Progression")
                .AddTrace(gens, obs.BestFitness, "Best", color: "#2ecc71")
                .AddTrace(gens, obs.AverageFitness, "Average", color: "#3498db")
                .AddTrace(gens, obs.WorstFitness, "Worst", color: "#e74c3c", dash: "dot")
                .XAxis("Generation").YAxis("Fitness").Height(400)
                .LineHoverLabels("Gen", "Fitness");

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

            db.AddChart(fitnessChart);
            db.AddNote("Best: highest-fitness individual each generation. Average: population mean. Worst: lowest-fitness individual. The shaded band spans ±1 standard deviation around the average — a narrowing band over time indicates the population is converging on high-quality solutions.");

            db.AddChart(diversityChart);
            db.AddNote($"Percentage of genetically distinct individuals out of a population of {popSize}. A high value means the search space is still being broadly explored. A sudden drop signals premature convergence — if it occurs before fitness improves significantly, the algorithm may be stuck in a local optimum.");

            if (obs.FitnessStandardDeviation.Count == genCount)
            {
                db.AddChart(convergenceChart);
                db.AddNote("Standard deviation of fitness across the population. High σ: individuals span a wide fitness range (active exploration). σ → 0: nearly all individuals share the same fitness (converged). A smooth, steady decay is healthy; a flat plateau at high σ indicates stagnation.");
            }

            var boxChart = new ChartBuilder(ChartType.Box)
                .Title("Fitness Distribution per Generation (sampled)")
                .XAxis("Generation").YAxis("Fitness").Height(400);
            var boxStep = genCount > 50 ? (int)Math.Ceiling(genCount / 50.0) : 1;
            for (var g = 0; g < genCount; g += boxStep)
                if (g < obs.AllGenerations.Count)
                    boxChart.AddGroup(g.ToString(), obs.AllGenerations[g].Select(s => s.Fitness).ToArray());

            db.AddChart(boxChart);
            db.AddNote("Box-and-whisker plots of the full fitness distribution sampled across generations. The box spans Q1–Q3; whiskers reach the extremes; the inner dot marks the mean. A rising, narrowing box over time indicates healthy convergence toward high-quality solutions.");
        }
        else
        {
            // ── Multi-objective: Population Statistics tab (diversity + convergence only) ──
            db.AddTab("Population Statistics");
            db.AddNote($"<strong>Algorithm: {algorithmLabel}</strong> — Fitness progression and distribution charts are omitted here because the scalar fitness is an internal bookkeeping value, not the primary optimisation metric for multi-objective runs. For Pareto fronts, hypervolume, and objective trade-offs, use the <em>Pareto Front</em> tab.");

            db.AddChart(diversityChart);
            db.AddNote($"Percentage of genetically distinct individuals out of a population of {popSize}. A high value means the search space is still being broadly explored. Genetic diversity is meaningful regardless of algorithm type — a premature drop can indicate the population has collapsed before the Pareto front has converged.");

            if (obs.FitnessStandardDeviation.Count == genCount)
            {
                db.AddChart(convergenceChart);
                db.AddNote("Standard deviation of the scalar fitness proxy across the population. While the scalar fitness is secondary in multi-objective runs, σ still tracks whether the population is spread out (high) or tightly clustered (low). Use in conjunction with the front-size and hypervolume charts in the Pareto Front tab for a complete picture.");
            }
        }

        // ── Tab 2: Parameter Influence ────────────────────────────────────────
        if (obs.GeneSchema is { Length: > 0 } && obs.AllGenerations.Count > 0)
        {
            var schemaLen = obs.GeneSchema.Length;
            var allTagged = obs.AllGenerations
                .SelectMany((g, gi) => g.Select((s, si) => (snap: s, gi, si, total: g.Count)))
                .ToArray();
            var allSnaps = allTagged.Select(t => t.snap).ToArray();

            // TickValues[i] corresponds to GeneSchema[i] (schema position, NOT GenePoolIndex).
            // GenePoolIndex is a per-component index and must not be used to index TickValues.
            var dataReady = allSnaps.All(s => s.TickValues != null && s.TickValues.Length >= schemaLen);
            if (dataReady)
            {
                db.AddTab("Parameter Influence");

                // Build normalised gene-value vectors indexed by schema position.
                var geneValsAll = new double[schemaLen][];
                for (var gi = 0; gi < schemaLen; gi++)
                {
                    var tc = obs.GeneSchema[gi].TickCount;
                    geneValsAll[gi] = allSnaps
                        .Select(s => tc > 1 ? (double)s.TickValues[gi] / (tc - 1) : 0.0)
                        .ToArray();
                }

                if (!isMultiObjective)
                {
                    // ── Single-objective: correlate each parameter against fitness ────
                    var allFitness = allSnaps.Select(s => s.Fitness).ToArray();
                    var allGens = allTagged.Select(t => (double)t.snap.Generation).ToArray();
                    var allHover = allTagged
                        .Select(t => $"Gen: {t.gi} | #Ind {t.si}/{t.total} | Fitness: {t.snap.Fitness:F4}")
                        .ToArray();

                    var corrData = obs.GeneSchema
                        .Select((gene, gi) => (gene, gi, corr: SpearmanCorrelation(geneValsAll[gi], allFitness)))
                        .ToArray();

                    // Correlation bar — the most actionable chart; shown first.
                    db.AddChart(new ChartBuilder(ChartType.Bar)
                        .Title("Parameter–Fitness Correlation (Spearman ρ)")
                        .Categories(corrData.Select(x => x.gene.GeneName), corrData.Select(x => x.corr))
                        .BarColors(corrData.Select(x => x.corr >= 0 ? "rgba(55,128,191,0.85)" : "rgba(219,64,82,0.85)"))
                        .Horizontal()
                        .Height(Math.Max(300, schemaLen * 35 + 80)));
                    db.AddNote("<strong>Blue (positive ρ):</strong> increasing this parameter tends to improve fitness. <strong>Red (negative ρ):</strong> decreasing it tends to help. Values near 0 indicate no clear relationship. Focus tuning effort on the parameters with the highest |ρ| — they have the most leverage on the outcome.");

                    // Scatter for the top 3 most correlated parameters only.
                    var top3 = corrData.OrderByDescending(x => Math.Abs(x.corr)).Take(3).ToArray();
                    foreach (var (gene, gi, _) in top3)
                    {
                        db.AddChart(new ChartBuilder(ChartType.Scatter)
                            .Title($"{gene.GeneName} vs Fitness")
                            .Data(geneValsAll[gi], allFitness)
                            .Color(allGens, colorBarTitle: "Generation")
                            .Text(allHover)
                            .XAxis($"{gene.GeneName} (0–1 normalised)").YAxis("Fitness")
                            .UseWebGL().Opacity(0.5).MarkerSize(3).Height(350));
                    }
                    if (top3.Length > 0)
                        db.AddNote("Scatter of the top 3 parameters by |ρ|, showing every individual across all generations. Color encodes generation (earlier = dark, later = bright). A band or cluster of late-generation points in a narrow parameter range indicates where the algorithm converged. Hover over a point to see its generation, index, and fitness.");
                }
                else
                {
                    // ── Multi-objective: one correlation bar per objective ────────────
                    var objNames = obs.ObjectiveNames!;
                    db.AddNote($"<strong>Algorithm: {algorithmLabel}</strong> — Parameter correlations are computed against each objective individually, not the scalar fitness proxy. A high |ρ| means this parameter strongly drives that objective. Use these together with the Pareto Front tab to understand which parameters cause trade-offs between objectives.");

                    for (var m = 0; m < objNames.Length; m++)
                    {
                        var mi = m;
                        var objVals = allSnaps
                            .Select(s => s.Objectives != null && s.Objectives.Length > mi ? s.Objectives[mi] : 0.0)
                            .ToArray();

                        var corrData = obs.GeneSchema
                            .Select((gene, gi) => (gene, corr: SpearmanCorrelation(geneValsAll[gi], objVals)))
                            .ToArray();

                        db.AddChart(new ChartBuilder(ChartType.Bar)
                            .Title($"Parameter Correlation — {objNames[m]} (Spearman ρ)")
                            .Categories(corrData.Select(x => x.gene.GeneName), corrData.Select(x => x.corr))
                            .BarColors(corrData.Select(x => x.corr >= 0 ? "rgba(55,128,191,0.85)" : "rgba(219,64,82,0.85)"))
                            .Horizontal()
                            .Height(Math.Max(300, schemaLen * 35 + 80)));
                    }
                    if (objNames.Length > 1)
                        db.AddNote("Compare correlation bars across objectives: a parameter that is blue for one objective and red for another is creating a trade-off — you cannot improve both by moving it in the same direction. That tension is the source of the Pareto front.");
                }

                // ── Parallel coordinates — final generation ──────────────────
                var lastGenParam = obs.AllGenerations[obs.AllGenerations.Count - 1];
                var lastGenValid = lastGenParam
                    .Where(s => s.TickValues != null && s.TickValues.Length >= schemaLen)
                    .ToArray();

                if (lastGenValid.Length > 0)
                {
                    var pcParam = new ChartBuilder(ChartType.Parcoords)
                        .Title($"Parameter Space — Final Generation (Gen {genCount - 1})")
                        .Height(500);

                    for (var gi = 0; gi < schemaLen; gi++)
                    {
                        var tc = obs.GeneSchema[gi].TickCount;
                        var dimVals = lastGenValid
                            .Select(s => tc > 1 ? (double)s.TickValues[gi] / (tc - 1) : 0.0)
                            .ToArray();
                        pcParam.AddDimension(obs.GeneSchema[gi].GeneName, dimVals, 0, 1);
                    }

                    // Color by fitness (single-obj) or negated Pareto rank (multi-obj, rank 0 = brightest).
                    if (isMultiObjective)
                    {
                        var rankVals = lastGenValid.Select(s => (double)-s.ParetoRank).ToArray();
                        var hoverLabels = lastGenValid
                            .Select((s, si) => $"#Ind {si}/{lastGenValid.Length} | Rank: {s.ParetoRank} | Fitness: {s.Fitness:F4}")
                            .ToArray();
                        pcParam.AddDimension("Pareto Rank", lastGenValid.Select(s => (double)s.ParetoRank).ToArray());
                        pcParam.Color(rankVals, "Viridis", "Rank (0=best)")
                               .FilterList(hoverLabels);
                    }
                    else
                    {
                        var fitnessVals = lastGenValid.Select(s => s.Fitness).ToArray();
                        var hoverLabels = lastGenValid
                            .Select((s, si) => $"#Ind {si}/{lastGenValid.Length} | Fitness: {s.Fitness:F4}")
                            .ToArray();
                        pcParam.AddDimension("Fitness", fitnessVals);
                        pcParam.Color(fitnessVals, "RdYlBu", "Fitness")
                               .ReverseScale()
                               .FilterList(hoverLabels);
                    }

                    db.AddChart(pcParam);
                    db.AddNote(isMultiObjective
                        ? "Each line is one individual in the final generation, colored by Pareto rank (bright = rank 0, non-dominated). Drag axis handles to brush a range and filter visible lines — e.g. restrict all axes to find the parameter region that consistently produces front-rank solutions. The Highlight control isolates a specific individual by index."
                        : "Each line is one individual in the final generation, colored by fitness (red = high, blue = low). Drag axis handles to define a parameter region and see which individuals fall in it. The Highlight control lets you isolate a specific individual to inspect its exact parameter values.");
                }
            }
        }

        // ── Tab 3: Individual Explorer ────────────────────────────────────────
        if (obs.AllGenerations.Count > 0)
        {
            var schemaForExplorer = obs.GeneSchema ?? Array.Empty<GeneSchema>();
            var explorerTagged = obs.AllGenerations
                .SelectMany((g, gi) => g.Select((s, si) => (snap: s, gi, si, total: g.Count)))
                .ToArray();
            var allSnaps = explorerTagged.Select(t => t.snap).ToArray();

            // Build per-point popup HTML
            var popupData = explorerTagged.Select(t =>
                BuildIndividualPopup(t.snap, t.gi, t.si, t.total, schemaForExplorer,
                    isMultiObjective ? obs.ObjectiveNames : null)).ToArray();

            // Hover text (brief, for tooltip)
            var explorerHover = explorerTagged
                .Select(t => $"Gen {t.gi} · #{t.si} — click for details")
                .ToArray();

            var explorerChart = new ChartBuilder(ChartType.Scatter)
                .Title("All Individuals — Fitness over Generations")
                .Data(allSnaps.Select(s => (double)s.Generation), allSnaps.Select(s => s.Fitness))
                .Color(allSnaps.Select(s => (double)s.Generation), colorBarTitle: "Generation")
                .Text(explorerHover)
                .CustomData(popupData)
                .XAxis("Generation").YAxis("Fitness")
                .UseWebGL().Opacity(0.5).MarkerSize(3).Height(450);

            // For NSGA: add an objective-switcher dropdown via Plotly updatemenus
            if (isMultiObjective && obs.ObjectiveNames is { Length: > 0 })
            {
                var objNames = obs.ObjectiveNames!;
                // Precompute y arrays for each objective
                var yOptions = new List<(string label, double[] data)>
                {
                    ("Fitness (proxy)", allSnaps.Select(s => s.Fitness).ToArray())
                };
                for (var m = 0; m < objNames.Length; m++)
                {
                    var mi = m;
                    yOptions.Add((objNames[m],
                        allSnaps.Select(s => s.Objectives != null && s.Objectives.Length > mi
                            ? s.Objectives[mi] : 0.0).ToArray()));
                }
                yOptions.Add(("Pareto Rank", allSnaps.Select(s => (double)s.ParetoRank).ToArray()));

                var buttons = yOptions.Select(opt => (object)new Dictionary<string, object?>
                {
                    ["label"] = opt.label,
                    ["method"] = "update",
                    ["args"] = new object[]
                    {
                        new Dictionary<string, object?> { ["y"] = new object[] { opt.data } },
                        new Dictionary<string, object?> { ["yaxis.title.text"] = opt.label }
                    }
                }).ToList();

                explorerChart.Layout(new Dictionary<string, object?>
                {
                    ["updatemenus"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"] = "dropdown",
                            ["active"] = 0,
                            ["buttons"] = buttons,
                            ["x"] = 0.0,
                            ["y"] = 1.13,
                            ["xanchor"] = "left",
                            ["yanchor"] = "top",
                            ["bgcolor"] = "var(--bg-tertiary)",
                            ["bordercolor"] = "var(--border-color)",
                            ["font"] = new Dictionary<string, object?> { ["color"] = "var(--text-primary)" }
                        }
                    },
                    ["margin"] = new Dictionary<string, object?> { ["t"] = 85, ["b"] = 50, ["l"] = 60, ["r"] = 20 }
                });
            }

            db.AddTab("Individual Explorer");
            db.AddChart(explorerChart);

            if (isMultiObjective)
                db.AddNote("Each dot is one individual evaluated during the run. Use the <strong>dropdown</strong> at the top-left to switch the Y axis between fitness proxy, each objective, and Pareto rank. <strong>Click any dot</strong> to open a sidebar with its exact parameter values and objective scores.");
            else
                db.AddNote("Each dot is one individual. Color encodes generation (darker = earlier, brighter = later). <strong>Click any dot</strong> to open a sidebar showing the full parameter breakdown for that individual.");

            if (isMultiObjective && obs.ObjectiveNames is { Length: > 0 } && obs.ObjectiveStats.Count == genCount)
            {
                // ── Multi-objective: per-objective progression with dropdown switcher ──
                var objNames = obs.ObjectiveNames!;

                // Precompute mean/min/max arrays for every objective
                var objMeans = new double[objNames.Length][];
                var objMins  = new double[objNames.Length][];
                var objMaxs  = new double[objNames.Length][];
                for (var m = 0; m < objNames.Length; m++)
                {
                    objMeans[m] = [.. gensD.Select(g => GetObjStat(obs, (int)g, m, 2))];
                    objMins[m]  = [.. gensD.Select(g => GetObjStat(obs, (int)g, m, 0))];
                    objMaxs[m]  = [.. gensD.Select(g => GetObjStat(obs, (int)g, m, 1))];
                }

                // Seed the chart with the first objective's traces
                var objTrajectory = new ChartBuilder(ChartType.Line)
                    .Title($"Objective Progression — {objNames[0]}")
                    .AddTrace(gens, objMeans[0], "Mean",  color: "#3498db", width: 2)
                    .AddTrace(gens, objMins[0],  "Min",   color: "#2ecc71", dash: "dot")
                    .AddTrace(gens, objMaxs[0],  "Max",   color: "#e74c3c", dash: "dot")
                    .XAxis("Generation").YAxis(objNames[0]).Height(320);

                // Build one dropdown button per objective
                var objButtons = objNames.Select((name, m) => (object)new Dictionary<string, object?>
                {
                    ["label"]  = name,
                    ["method"] = "update",
                    ["args"]   = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["y"] = new object[] { objMeans[m], objMins[m], objMaxs[m] }
                        },
                        new Dictionary<string, object?>
                        {
                            ["title.text"]       = $"Objective Progression — {name}",
                            ["yaxis.title.text"] = name
                        }
                    }
                }).ToList();

                objTrajectory.Layout(new Dictionary<string, object?>
                {
                    ["updatemenus"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"]      = "dropdown",
                            ["active"]    = 0,
                            ["buttons"]   = objButtons,
                            ["x"]         = 0.0,
                            ["y"]         = 1.15,
                            ["xanchor"]   = "left",
                            ["yanchor"]   = "top",
                            ["bgcolor"]   = "var(--bg-tertiary)",
                            ["bordercolor"] = "var(--border-color)",
                            ["font"]      = new Dictionary<string, object?> { ["color"] = "var(--text-primary)" }
                        }
                    },
                    ["margin"] = new Dictionary<string, object?> { ["t"] = 85, ["b"] = 50, ["l"] = 60, ["r"] = 20 }
                });

                db.AddChart(objTrajectory);
                db.AddNote("Use the <strong>dropdown</strong> to switch between objectives. Each chart shows the <strong>mean</strong> (solid), <strong>min</strong> (green dashed), and <strong>max</strong> (red dashed) value across the population per generation.");
            }
            else
            {
                var bestTrajectory = new ChartBuilder(ChartType.Line)
                    .Title("Best Individual Trajectory")
                    .AddTrace(gens, obs.BestFitness, "Best Fitness", color: "#2ecc71", width: 2)
                    .AddTrace(gens, obs.AverageFitness, "Average Fitness", color: "#3498db", dash: "dot")
                    .XAxis("Generation").YAxis("Fitness").Height(300);
                db.AddChart(bestTrajectory);
                db.AddNote("The green line tracks the best fitness found in each generation; the dashed blue line tracks the population mean. A narrowing gap between the two signals convergence.");
            }
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
                pcFinalObj.Color(withObj.Select(s => (double)s.ParetoRank), colorBarTitle: "Pareto Rank")
                          .FilterList(paretoHover);
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

                    var allGenHover = sampled.Select((s, si) =>
                    {
                        var objStr = string.Join(" | ", s.Objectives!.Select((o, i) =>
                            $"{(i < objNames.Length ? objNames[i] : $"Obj{i}")}: {o:F4}"));
                        return $"Gen {s.Generation} | Rank: {s.ParetoRank} | {objStr}";
                    }).ToArray();

                    var pcAllObj = new ChartBuilder(ChartType.Parcoords)
                        .Title("All Generations — Objective Space Evolution")
                        .Height(500);
                    for (var m = 0; m < objNames.Length; m++)
                    {
                        var mi = m;
                        pcAllObj.AddDimension(objNames[m], sampled.Select(s => s.Objectives![mi]).ToArray());
                    }
                    pcAllObj.AddDimension("Generation", sampled.Select(s => (double)s.Generation).ToArray());
                    pcAllObj.Color(sampled.Select(s => (double)s.Generation), colorBarTitle: "Generation")
                            .FilterList(allGenHover);
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

    /// <summary>
    ///     Builds the HTML content injected into the individual details sidebar popup.
    /// </summary>
    private static string BuildIndividualPopup(IndividualSnapshot s, int genIdx, int snapIdx, int total,
        GeneSchema[] schema, string[]? objNames)
    {
        var sb = new System.Text.StringBuilder();

        // ── Identity ────────────────────────────────────────────────────────
        sb.Append("<div class='pop-section'>");
        sb.Append("<div class='pop-label'>Generation &amp; Index</div>");
        sb.Append($"<div class='pop-value'>Generation {genIdx} &middot; Individual {snapIdx + 1} of {total}</div>");
        sb.Append("</div>");

        // ── Fitness ─────────────────────────────────────────────────────────
        sb.Append("<div class='pop-section'>");
        sb.Append("<div class='pop-label'>Fitness</div>");
        sb.Append($"<div class='pop-value'>{s.Fitness:F6}</div>");
        sb.Append("</div>");

        // ── Multi-objective extras ───────────────────────────────────────────
        if (objNames is { Length: > 0 } && s.Objectives != null)
        {
            sb.Append("<hr class='pop-divider'>");
            sb.Append("<div class='pop-section'>");
            sb.Append("<div class='pop-label'>Objectives</div>");
            sb.Append("<table class='pop-table'>");
            for (var i = 0; i < s.Objectives.Length; i++)
            {
                var name = i < objNames.Length ? objNames[i] : $"Obj {i}";
                sb.Append($"<tr><td>{EscapePopup(name)}</td><td>{s.Objectives[i]:F6}</td></tr>");
            }
            sb.Append("</table>");
            sb.Append("</div>");

            sb.Append("<div class='pop-section'>");
            sb.Append("<div class='pop-label'>Pareto Rank</div>");
            sb.Append($"<div class='pop-value'>{s.ParetoRank}</div>");
            sb.Append("</div>");

            if (!double.IsInfinity(s.CrowdingDistance))
            {
                sb.Append("<div class='pop-section'>");
                sb.Append("<div class='pop-label'>Crowding Distance</div>");
                sb.Append($"<div class='pop-value'>{s.CrowdingDistance:F4}</div>");
                sb.Append("</div>");
            }
        }

        // ── Parameters ──────────────────────────────────────────────────────
        if (schema.Length > 0 && s.TickValues != null && s.TickValues.Length >= schema.Length)
        {
            sb.Append("<hr class='pop-divider'>");
            sb.Append("<div class='pop-section'>");
            sb.Append("<div class='pop-label'>Parameters</div>");
            sb.Append("<table class='pop-table'>");
            for (var i = 0; i < schema.Length; i++)
            {
                var gene = schema[i];
                var tick = s.TickValues[i];
                var normalised = gene.TickCount > 1 ? tick / (double)(gene.TickCount - 1) : 0.0;
                sb.Append($"<tr><td>{EscapePopup(gene.GeneName)}</td><td>{tick} / {gene.TickCount - 1}&nbsp;<span style='opacity:.55'>({normalised:P0})</span></td></tr>");
            }
            sb.Append("</table>");
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    private static string EscapePopup(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

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
