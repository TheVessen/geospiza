using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GeospizaCore.Core;
using GeospizaCore.Strategies;

namespace GeospizaPlugin.AI;

public static class PromptBuilder
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    private static string LoadPrompt(string filename)
    {
        var resourceName = $"GeospizaPlugin.Src.AI.Prompts.{filename}";
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return $"[Missing prompt file: {filename}]";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string Build(EvolutionObserver obs, string userPrompt, AnalysisMode mode)
    {
        var sb = new StringBuilder();
        var isMultiObjective = obs != null && obs.Algorithm != EvolutionObserver.AlgorithmType.SingleObjective;

        sb.AppendLine(LoadPrompt("system.md"));
        sb.AppendLine(LoadPrompt($"mode-{ModeFileName(mode)}.md"));

        if (mode != AnalysisMode.Explanation)
        {
            if (mode == AnalysisMode.SettingsFeedback)
                sb.AppendLine(LoadPrompt("strategies.md"));
            sb.AppendLine("## Run Data");
            AppendSettings(sb, obs);

            switch (mode)
            {
                case AnalysisMode.LastGeneration:
                    AppendLastGenerationData(sb, obs, isMultiObjective);
                    break;
                case AnalysisMode.SettingsFeedback:
                    AppendSettingsFeedbackData(sb, obs, isMultiObjective);
                    break;
            }
        }

        sb.AppendLine("## User Question");
        sb.AppendLine(string.IsNullOrWhiteSpace(userPrompt)
            ? DefaultQuestion(mode, isMultiObjective)
            : userPrompt);

        return sb.ToString();
    }

    // Keep old signature working for the debug prompt output
    public static string Build(EvolutionObserver obs, string userPrompt)
        => Build(obs, userPrompt, AnalysisMode.SettingsFeedback);

    private static string ModeFileName(AnalysisMode mode) => mode switch
    {
        AnalysisMode.LastGeneration  => "last-generation",
        AnalysisMode.SettingsFeedback => "settings-feedback",
        AnalysisMode.Explanation     => "explanation",
        _                            => "settings-feedback"
    };

    private static string DefaultQuestion(AnalysisMode mode, bool isMultiObjective) => mode switch
    {
        AnalysisMode.LastGeneration  => isMultiObjective
            ? "Describe the Pareto front and identify 2-3 individuals worth inspecting."
            : "Describe the best individual and the final population state.",
        AnalysisMode.SettingsFeedback => "What settings changes would improve the next run?",
        AnalysisMode.Explanation      => "How should I set up my fitness function to work well with Geospiza?",
        _                             => "Analyse this run."
    };

    private static void AppendSettings(StringBuilder sb, EvolutionObserver obs)
    {
        if (obs == null) return;
        var isMultiObjective = obs.Algorithm != EvolutionObserver.AlgorithmType.SingleObjective;

        sb.AppendLine($"Algorithm: {obs.Algorithm}  |  Generations: {obs.CurrentGenerationIndex}  |  TerminatedEarly: {obs.TerminatedEarly}");

        if (isMultiObjective && obs.ObjectiveNames != null && obs.ObjectiveNames.Length > 0)
            sb.AppendLine($"Objectives ({obs.ObjectiveNames.Length}): {string.Join(", ", obs.ObjectiveNames.Select((n, i) => $"[{i}]={n}"))}");

        if (obs.Settings != null)
        {
            var s = obs.Settings;
            sb.Append($"PopulationSize: {s.PopulationSize}  |  MaxGenerations: {s.MaxGenerations}");
            if (!isMultiObjective) sb.Append($"  |  EliteSize: {s.EliteSize}");
            sb.AppendLine();

            if (s.SelectionStrategy != null && !isMultiObjective)
            {
                var selParams = s.SelectionStrategy switch
                {
                    TournamentSelection ts =>
                        $"TournamentSize={ts.TournamentSize}",
                    _ => ""
                };
                sb.AppendLine($"Selection: {s.SelectionStrategy.GetType().Name}" +
                              (selParams.Length > 0 ? $"  |  {selParams}" : ""));
            }
            if (s.CrossoverStrategy != null)
                sb.AppendLine($"Crossover: {s.CrossoverStrategy.GetType().Name}  |  CrossoverRate={s.ConfiguredCrossoverRate}");
            if (s.MutationStrategy != null)
            {
                var mutParams = s.MutationStrategy switch
                {
                    FixedValueMutation fvm =>
                        $"MutationRate={s.ConfiguredMutationRate}  |  MutationValue={fvm.MutationValue}",
                    PercentageMutation pm =>
                        $"MutationRate={s.ConfiguredMutationRate}  |  MutationPercentage={pm.MutationPercentage}",
                    _ =>
                        $"MutationRate={s.ConfiguredMutationRate}"
                };
                sb.AppendLine($"Mutation: {s.MutationStrategy.GetType().Name}  |  {mutParams}");
            }
            if (s.PairingStrategy != null)
            {
                var pairParams = s.PairingStrategy switch
                {
                    PairingStrategy ps =>
                        $"InBreedingFactor={ps.InBreedingFactor}  |  DistanceFunction={ps.DistanceFunction}",
                    _ => ""
                };
                sb.AppendLine($"Pairing: {s.PairingStrategy.GetType().Name}" +
                              (pairParams.Length > 0 ? $"  |  {pairParams}" : ""));
            }
            if (s.TerminationStrategy != null)
            {
                AppendTerminationParams(sb, s.TerminationStrategy, indent: "");

                // Flag termination strategies that use scalar fitness — misleading for multi-objective
                if (isMultiObjective)
                {
                    var scalarTerminators = GetScalarTerminatorNames(s.TerminationStrategy);
                    if (scalarTerminators.Count > 0)
                        sb.AppendLine($"CONFIG-WARNING: {string.Join(", ", scalarTerminators)} use scalar fitness to decide when to stop, which is unreliable for multi-objective runs. Prefer PopulationDiversity.");
                }
            }

            // NSGA-III: emit divisions, reference point count, and coverage ratio
            if (obs.Algorithm == EvolutionObserver.AlgorithmType.NsgaIII && s.ReferencePointDivisions > 0)
            {
                var objCount = obs.ObjectiveNames?.Length ?? 0;
                var refCount = objCount > 0
                    ? ParetoUtils.ReferencePointCount(objCount, s.ReferencePointDivisions)
                    : 0;
                sb.Append($"NSGA-III: Divisions={s.ReferencePointDivisions}");
                if (refCount > 0)
                {
                    sb.Append($"  |  ReferencePoints={refCount}");
                    var coverage = (double)s.PopulationSize / refCount;
                    sb.Append($"  |  Pop/RefPoint={coverage:F2}");
                    if (coverage < 1.0)
                        sb.Append("  ← CONFIG-WARNING: population smaller than reference point count — many reference points will stay empty, causing unstable diversity.");
                    else if (coverage < 2.0)
                        sb.Append("  ← NOTE: population barely covers reference points. Consider increasing population or reducing divisions.");
                }
                sb.AppendLine();
            }
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Emits termination strategy parameters, expanding CompositeTermination into its components.
    /// </summary>
    private static void AppendTerminationParams(StringBuilder sb, ITerminationStrategy t, string indent)
    {
        if (t is CompositeTermination ct)
        {
            sb.AppendLine($"{indent}Termination: CompositeTermination (fires when any sub-strategy triggers)");
            // Reflect into the private _strategies list to enumerate components
            var field = ct.GetType().GetField("_strategies",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(ct) is List<ITerminationStrategy> inner)
                foreach (var s in inner)
                    AppendTerminationParams(sb, s, indent + "  ");
            return;
        }

        var tParams = t switch
        {
            ProgressConvergence pc =>
                $"Threshold={pc.TerminationThreshold}  |  ProgressRange={pc.ProgressRange}",
            BestFitnessStagnation bfs =>
                $"Threshold={bfs.TerminationThreshold}  |  StagnationGenerations={bfs.StagnationGenerations}",
            PopulationDiversity pd =>
                $"Threshold={pd.TerminationThreshold}",
            _ => $"Threshold={t.TerminationThreshold}"
        };
        sb.AppendLine($"{indent}Termination: {t.GetType().Name}  |  {tParams}");
    }

    /// <summary>
    /// Returns the names of any scalar-fitness-based termination strategies, recursing into CompositeTermination.
    /// </summary>
    private static List<string> GetScalarTerminatorNames(ITerminationStrategy t)
    {
        var result = new List<string>();
        if (t is CompositeTermination ct)
        {
            var field = ct.GetType().GetField("_strategies",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(ct) is List<ITerminationStrategy> inner)
                foreach (var s in inner)
                    result.AddRange(GetScalarTerminatorNames(s));
            return result;
        }
        if (t is ProgressConvergence || t is BestFitnessStagnation)
            result.Add(t.GetType().Name);
        return result;
    }

    private static void AppendLastGenerationData(StringBuilder sb, EvolutionObserver obs, bool isMultiObjective)
    {
        var schema = obs.GeneSchema;

        // Gene influence table
        var geneSummary = obs.GeneSummary;
        if (geneSummary != null && schema != null && geneSummary.Length > 0)
        {
            var hasRealValues = schema.Any(s => !double.IsNaN(s.MinValue));
            sb.AppendLine("### Gene Influence (top 10 by |FitnessCorrelation|)");
            sb.AppendLine(hasRealValues
                ? "Gene            | Corr   | Mean       | Std        | Range"
                : "Gene            | Corr   | MeanTick | StdTick | TickRange");
            var ranked = geneSummary
                .Select((g, i) => new { g, sch = i < schema.Length ? schema[i] : null })
                .OrderByDescending(x => Math.Abs(x.g.FitnessCorrelation))
                .Take(10);
            foreach (var entry in ranked)
            {
                var g = entry.g;
                var sch = entry.sch;
                var label = sch != null ? $"{sch.GeneName}[{sch.GenePoolIndex}]" : g.GeneGuid.ToString().Substring(0, 8);
                if (hasRealValues && sch != null && !double.IsNaN(sch.MinValue))
                {
                    var meanVal = sch.TickToValue((int)Math.Round(g.MeanTick));
                    var minVal  = sch.TickToValue(g.MinTick);
                    var maxVal  = sch.TickToValue(g.MaxTick);
                    sb.AppendLine($"{label,-16} | {g.FitnessCorrelation,6:F3} | {meanVal,10:G4} | n/a        | {minVal:G4}-{maxVal:G4}");
                }
                else
                {
                    sb.AppendLine($"{label,-16} | {g.FitnessCorrelation,6:F3} | {g.MeanTick,8:F0} | {g.StdTick,7:F1} | {g.MinTick}-{g.MaxTick}");
                }
            }
            sb.AppendLine();
        }

        if (!isMultiObjective)
        {
            // Best individual with gene values
            var bestInd = obs.FinalBestIndividual;
            if (bestInd != null && schema != null)
            {
                sb.AppendLine($"### Best Individual  Fitness={bestInd.Fitness:F4}  Generation={bestInd.Generation}");
                var pool = bestInd.GenePool;
                var hasRealValues = schema.Any(s => !double.IsNaN(s.MinValue));
                sb.AppendLine(hasRealValues ? "Gene values (name[index]: value  [range min..max]):" : "Gene values (name[index]: tick/max):");
                for (var i = 0; i < Math.Min(pool.Count, schema.Length); i++)
                {
                    var s = schema[i];
                    var tick = pool[i].TickValue;
                    if (hasRealValues && !double.IsNaN(s.MinValue))
                    {
                        var realVal = s.TickToValue(tick);
                        sb.Append($"  {s.GeneName}[{s.GenePoolIndex}]:{realVal:G4} [{s.MinValue:G4}..{s.MaxValue:G4}]");
                    }
                    else
                    {
                        sb.Append($"  {s.GeneName}[{s.GenePoolIndex}]:{tick}/{s.TickCount}");
                    }
                    if ((i + 1) % 4 == 0) sb.AppendLine();
                }
                sb.AppendLine();
            }
        }
        else
        {
            // Pareto front individuals with objectives
            var pop = obs.FinalPopulationSnapshot;
            if (pop != null)
            {
                var front = pop.Where(ind => ind.ParetoRank == 0).ToList();
                if (obs.ObjectiveNames != null)
                    sb.AppendLine($"Objectives: {string.Join(", ", obs.ObjectiveNames.Select((n, i) => $"[{i}]={n}"))}");

                // Tell the AI the direction so it describes results correctly
                sb.AppendLine("Note: Geospiza always maximizes. Larger number = better, always, regardless of scale or sign. Do not judge by absolute value or assume any particular range. Say \"best on [objective]\" for the highest value, \"weakest on [objective]\" for the lowest.");

                sb.AppendLine($"Pareto front size: {front.Count}");
                sb.AppendLine("Id       | Gen | Fitness    | Objectives");
                for (var i = 0; i < Math.Min(front.Count, 20); i++)
                {
                    var ind = front[i];
                    var shortId = ind.Id.ToString().Substring(0, 8);
                    var objs = ind.Objectives != null
                        ? string.Join(", ", ind.Objectives.Select(o => o.ToString("F3")))
                        : "n/a";
                    sb.AppendLine($"{shortId} | {ind.Generation,3} | {ind.Fitness,10:F4} | [{objs}]");
                }
                sb.AppendLine();
            }
        }
    }

    private static void AppendSettingsFeedbackData(StringBuilder sb, EvolutionObserver obs, bool isMultiObjective)
    {
        if (isMultiObjective)
        {
            AppendMultiObjectiveFeedbackData(sb, obs);
        }
        else
        {
            AppendSingleObjectiveFeedbackData(sb, obs);
        }

        // Diversity — relevant for both
        var uniq = obs.NumberOfUniqueIndividuals;
        if (uniq != null && uniq.Count > 0)
        {
            var step = Math.Max(1, uniq.Count / 10);
            var samples = Enumerable.Range(0, uniq.Count)
                .Where(i => i % step == 0 || i == uniq.Count - 1)
                .Select(i => $"gen{i}:{uniq[i]}");
            sb.AppendLine($"UniqueIndividuals (sampled): {string.Join(", ", samples)}");
            sb.AppendLine();
        }

        // Gene influence — top 6 by variability across the final population
        // Note: for multi-objective runs the correlation is against the scalar fitness proxy, not individual objectives.
        var schema = obs.GeneSchema;
        var geneSummary = obs.GeneSummary;
        if (geneSummary != null && schema != null && geneSummary.Length > 0)
        {
            sb.AppendLine(isMultiObjective
                ? "### Gene Variability (top 6 by Std — correlation is vs scalar fitness proxy, not per-objective)"
                : "### Gene Influence (top 6)");
            sb.AppendLine("Gene            | Corr   | Std");
            var ranked = geneSummary
                .Select((g, i) => new { g, sch = i < schema.Length ? schema[i] : null })
                .OrderByDescending(x => isMultiObjective ? x.g.StdTick : Math.Abs(x.g.FitnessCorrelation))
                .Take(6);
            foreach (var entry in ranked)
            {
                var g = entry.g;
                var sch = entry.sch;
                var label = sch != null ? $"{sch.GeneName}[{sch.GenePoolIndex}]" : g.GeneGuid.ToString().Substring(0, 8);
                sb.AppendLine($"{label,-16} | {g.FitnessCorrelation,6:F3} | {g.StdTick,4:F1}");
            }
            sb.AppendLine();
        }
    }

    private static void AppendSingleObjectiveFeedbackData(StringBuilder sb, EvolutionObserver obs)
    {
        var best = obs.BestFitness;
        var avg  = obs.AverageFitness;
        if (best == null || best.Count == 0) return;

        sb.AppendLine("### Fitness Curve (sampled)");
        sb.AppendLine("Gen | BestFitness       | AvgFitness");
        var step = Math.Max(1, best.Count / 10);
        for (var i = 0; i < best.Count; i += step)
        {
            var avgVal = (avg != null && i < avg.Count) ? avg[i].ToString("F2") : "n/a";
            sb.AppendLine($"{i,3} | {best[i],17:F4} | {avgVal}");
        }
        var last = best.Count - 1;
        if (last % step != 0)
        {
            var avgLast = (avg != null && last < avg.Count) ? avg[last].ToString("F2") : "n/a";
            sb.AppendLine($"{last,3} | {best[last],17:F4} | {avgLast}");
        }
        sb.AppendLine();

        if (avg != null)
        {
            var spikes = avg.Count(a => Math.Abs(a) > Math.Abs(best[0]) * 10);
            if (spikes > 0)
                sb.AppendLine($"Note: {spikes} generations had average fitness >10x worse than gen0 best — likely constraint violations.");
        }
    }

    private static void AppendMultiObjectiveFeedbackData(StringBuilder sb, EvolutionObserver obs)
    {
        var hv          = obs.Hypervolume;
        var frontSizes  = obs.ParetoFrontSizes;
        var popSize     = obs.Settings?.PopulationSize ?? 0;

        // Hypervolume + front size as the primary progress signal
        if (hv != null && hv.Count > 0 && frontSizes != null && frontSizes.Count > 0)
        {
            sb.AppendLine("### Multi-Objective Progress (sampled)");
            sb.AppendLine("Note: scalar BestFitness is NOT the primary signal here — use Hypervolume and FrontSize.");
            sb.AppendLine("Gen | Hypervolume  | FrontSize | FrontFull%");
            var step = Math.Max(1, hv.Count / 10);
            var indices = Enumerable.Range(0, hv.Count)
                .Where(i => i % step == 0 || i == hv.Count - 1)
                .ToList();
            foreach (var i in indices)
            {
                var fs   = i < frontSizes.Count ? frontSizes[i] : 0;
                var pct  = popSize > 0 ? (fs * 100 / popSize) : 0;
                sb.AppendLine($"{i,3} | {hv[i],12:F4} | {fs,9} | {pct,9}%");
            }
            sb.AppendLine();

            // Summarise trend
            var hvFirst = hv[0];
            var hvLast  = hv[hv.Count - 1];
            var hvGain  = hvFirst > 0 ? (hvLast - hvFirst) / hvFirst * 100 : double.NaN;
            sb.Append($"HV trend: {hvFirst:F4} → {hvLast:F4}");
            if (!double.IsNaN(hvGain))
                sb.Append($"  ({hvGain:+0.#;-0.#;0}% change)");
            sb.AppendLine();

            // Flag stagnation: HV unchanged in last quarter of run
            var quarterStart = hv.Count * 3 / 4;
            var hvQuarterDelta = Math.Abs(hvLast - hv[quarterStart]);
            if (hvQuarterDelta < 1e-6 && hv.Count >= 8)
                sb.AppendLine("Warning: Hypervolume did not change in the final quarter of the run — the search has stalled.");

            // Flag if front filled the whole population (crowding may be an issue)
            var finalFrontSize = frontSizes[frontSizes.Count - 1];
            if (popSize > 0 && finalFrontSize >= popSize)
                sb.AppendLine("Warning: Pareto front covers 100% of the population — no dominated individuals remain. Consider increasing PopulationSize or adding constraints.");
            sb.AppendLine();
        }

        // Per-objective range across the final front — shows how well the trade-off space is covered
        var pop = obs.FinalPopulationSnapshot;
        var objNames = obs.ObjectiveNames;
        if (pop != null && pop.Length > 0 && pop[0].Objectives != null)
        {
            var front = pop.Where(ind => ind.ParetoRank == 0).ToArray();
            if (front.Length > 0)
            {
                var objCount = front[0].Objectives.Length;
                sb.AppendLine("### Objective Range Across Final Pareto Front");
                sb.AppendLine("Objective       | Min    | Max    | Spread");
                for (var m = 0; m < objCount; m++)
                {
                    var vals  = front.Select(ind => ind.Objectives[m]).ToArray();
                    var oMin  = vals.Min();
                    var oMax  = vals.Max();
                    var name  = (objNames != null && m < objNames.Length) ? objNames[m] : $"obj[{m}]";
                    sb.AppendLine($"{name,-16} | {oMin,6:F3} | {oMax,6:F3} | {oMax - oMin,6:F3}");
                }

                // Crowding distance: low spread = front is clustered, not well-distributed
                var crowding = front.Select(ind => ind.CrowdingDistance).Where(d => !double.IsInfinity(d)).ToArray();
                if (crowding.Length > 0)
                {
                    var cdMean = crowding.Average();
                    var cdMin  = crowding.Min();
                    sb.AppendLine($"CrowdingDistance (non-∞): mean={cdMean:F3}  min={cdMin:F3}");
                    if (cdMin < 0.01)
                        sb.AppendLine("Note: very low minimum crowding distance — front solutions are clustered in places. Spread may be poor.");
                }
                sb.AppendLine();
            }
        }

        // Constraint violation proxy — avg fitness spikes vs best at gen0
        var best = obs.BestFitness;
        var avg  = obs.AverageFitness;
        if (avg != null && best != null && best.Count > 0)
        {
            var spikes = avg.Count(a => Math.Abs(a) > Math.Abs(best[0]) * 10);
            if (spikes > 0)
                sb.AppendLine($"Note: {spikes} generations had average fitness >10x worse than gen0 best — likely constraint violations.");
        }
    }
}
