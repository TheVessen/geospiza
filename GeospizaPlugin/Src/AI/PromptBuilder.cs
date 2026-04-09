using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GeospizaCore.Core;

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
        sb.AppendLine($"Algorithm: {obs.Algorithm}  |  Generations: {obs.CurrentGenerationIndex}  |  TerminatedEarly: {obs.TerminatedEarly}");
        if (obs.Settings != null)
        {
            var s = obs.Settings;
            sb.AppendLine($"PopulationSize: {s.PopulationSize}  |  MaxGenerations: {s.MaxGenerations}  |  EliteSize: {s.EliteSize}");
            sb.AppendLine($"MutationRate: {s.ConfiguredMutationRate}  |  CrossoverRate: {s.ConfiguredCrossoverRate}");
            if (s.SelectionStrategy != null)
                sb.AppendLine($"Selection: {s.SelectionStrategy.GetType().Name}  |  Crossover: {s.CrossoverStrategy?.GetType().Name}  |  Mutation: {s.MutationStrategy?.GetType().Name}");
        }
        sb.AppendLine();
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
                sb.AppendLine($"Pareto front size: {front.Count}");
                sb.AppendLine("Idx | Fitness    | Objectives");
                for (var i = 0; i < Math.Min(front.Count, 20); i++)
                {
                    var ind = front[i];
                    var objs = ind.Objectives != null
                        ? string.Join(", ", ind.Objectives.Select(o => o.ToString("F3")))
                        : "n/a";
                    sb.AppendLine($"{i,3} | {ind.Fitness,10:F4} | [{objs}]");
                }
                sb.AppendLine();
            }
        }
    }

    private static void AppendSettingsFeedbackData(StringBuilder sb, EvolutionObserver obs, bool isMultiObjective)
    {
        // Fitness curve
        var best = obs.BestFitness;
        var avg  = obs.AverageFitness;
        if (best != null && best.Count > 0)
        {
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

            // Flag constraint violations
            if (avg != null)
            {
                var spikes = avg.Count(a => Math.Abs(a) > Math.Abs(best[0]) * 10);
                if (spikes > 0)
                    sb.AppendLine($"Note: {spikes} generations had average fitness >10x worse than gen0 best — likely constraint violations.");
            }
        }

        // Diversity
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

        // Gene influence (fewer entries — just top 6 for context)
        var schema = obs.GeneSchema;
        var geneSummary = obs.GeneSummary;
        if (geneSummary != null && schema != null && geneSummary.Length > 0)
        {
            sb.AppendLine("### Gene Influence (top 6)");
            sb.AppendLine("Gene            | Corr   | Std");
            var ranked = geneSummary
                .Select((g, i) => new { g, sch = i < schema.Length ? schema[i] : null })
                .OrderByDescending(x => Math.Abs(x.g.FitnessCorrelation))
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

        // Pareto front size trend for multi-objective
        if (isMultiObjective)
        {
            var frontSizes = obs.ParetoFrontSizes;
            if (frontSizes != null && frontSizes.Count > 0)
            {
                var step = Math.Max(1, frontSizes.Count / 10);
                var samples = Enumerable.Range(0, frontSizes.Count)
                    .Where(i => i % step == 0 || i == frontSizes.Count - 1)
                    .Select(i => $"gen{i}:{frontSizes[i]}");
                sb.AppendLine($"ParetoFrontSizes (sampled): {string.Join(", ", samples)}");
            }
        }
    }
}
