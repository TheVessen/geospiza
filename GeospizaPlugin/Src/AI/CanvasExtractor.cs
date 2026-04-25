using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GeospizaPlugin.AI;

/// <summary>
///     Extracts a structured, LLM-friendly description of a Grasshopper canvas focused on
///     the optimization model: every component upstream of the fitness component, plus a
///     count summary of the rest.
///     The fitness-upstream subgraph is what actually drives the search; everything else
///     (decorative geometry, panels, viewport-only previews) is collapsed into a category
///     summary so the LLM gets context without drowning in noise.
/// </summary>
public static class CanvasExtractor
{
    private const int MaxNodesIncluded = 80;

    /// <summary>
    ///     Renders the canvas as a markdown-flavoured text block ready to feed into the
    ///     summarization prompt. Returns null if no fitness component is found.
    /// </summary>
    public static string? Extract(GH_Document doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        var fitness = FindFitnessComponent(doc);
        if (fitness == null) return null;

        var upstream = WalkUpstream(fitness);

        var sb = new StringBuilder();
        sb.AppendLine($"Canvas: {doc.DisplayName ?? "Unnamed"}");
        sb.AppendLine($"Total objects: {doc.Objects.Count}");
        sb.AppendLine($"Fitness component: {DescribeObject(fitness)}");
        sb.AppendLine();

        // Upstream subgraph — the optimization model.
        sb.AppendLine("## Optimization model (fitness-upstream subgraph)");
        sb.AppendLine($"Nodes in subgraph: {upstream.Count}");

        // Count gene sources — sliders + Galapagos gene pools — separately because they're
        // the search variables and the LLM should know how many degrees of freedom there are.
        var geneSources = upstream
            .Where(IsGeneSource)
            .ToList();
        if (geneSources.Count > 0)
        {
            sb.AppendLine($"Gene sources: {geneSources.Count} ({SummariseGeneSources(geneSources)})");
        }

        sb.AppendLine();
        sb.AppendLine("Subgraph (each line = one node, '<-' lists immediate inputs):");
        var includeLimit = Math.Min(upstream.Count, MaxNodesIncluded);
        for (var i = 0; i < includeLimit; i++)
        {
            sb.AppendLine(DescribeNodeWithInputs(upstream[i]));
        }
        if (upstream.Count > includeLimit)
            sb.AppendLine($"... and {upstream.Count - includeLimit} more nodes (omitted to keep prompt concise)");

        // Decorative / unrelated components — just a count by category.
        sb.AppendLine();
        sb.AppendLine("## Other canvas content (not in fitness subgraph)");
        var upstreamSet = new HashSet<Guid>(upstream.Select(o => o.InstanceGuid));
        var others = doc.Objects.Where(o => !upstreamSet.Contains(o.InstanceGuid) && o.InstanceGuid != fitness.InstanceGuid).ToList();
        if (others.Count == 0)
        {
            sb.AppendLine("(none)");
        }
        else
        {
            var byCategory = others
                .GroupBy(GetCategory)
                .OrderByDescending(g => g.Count())
                .Take(10);
            foreach (var group in byCategory)
                sb.AppendLine($"- {group.Key}: {group.Count()}");
        }

        return sb.ToString();
    }

    private static IGH_DocumentObject? FindFitnessComponent(GH_Document doc)
    {
        foreach (var obj in doc.Objects)
        {
            var typeName = obj.GetType().Name;
            if (typeName == "GH_Fitness" || typeName == "GH_MultiObjectiveFitness")
                return obj;
        }
        return null;
    }

    /// <summary>
    ///     Breadth-first walk from <paramref name="root"/> following input wires backwards.
    ///     Returns nodes in approximate distance-from-fitness order, capped to keep prompts bounded.
    /// </summary>
    private static List<IGH_DocumentObject> WalkUpstream(IGH_DocumentObject root)
    {
        var visited = new HashSet<Guid>();
        var ordered = new List<IGH_DocumentObject>();
        var queue = new Queue<IGH_DocumentObject>();
        queue.Enqueue(root);
        visited.Add(root.InstanceGuid);

        while (queue.Count > 0 && ordered.Count < MaxNodesIncluded * 2)
        {
            var current = queue.Dequeue();
            ordered.Add(current);

            foreach (var src in GetInputSources(current))
            {
                if (visited.Add(src.InstanceGuid))
                    queue.Enqueue(src);
            }
        }

        return ordered;
    }

    private static IEnumerable<IGH_DocumentObject> GetInputSources(IGH_DocumentObject obj)
    {
        if (obj is IGH_Component component)
        {
            foreach (var p in component.Params.Input)
                foreach (var s in p.Sources)
                    if (s.Attributes?.GetTopLevel?.DocObject is IGH_DocumentObject top)
                        yield return top;
        }
        else if (obj is IGH_Param param)
        {
            foreach (var s in param.Sources)
                if (s.Attributes?.GetTopLevel?.DocObject is IGH_DocumentObject top)
                    yield return top;
        }
    }

    private static string DescribeNodeWithInputs(IGH_DocumentObject obj)
    {
        var label = DescribeObject(obj);
        var sources = GetInputSources(obj)
            .Select(s => s.NickName ?? s.GetType().Name)
            .Distinct()
            .Take(6)
            .ToList();

        return sources.Count == 0
            ? $"- {label}"
            : $"- {label}  <-  {string.Join(", ", sources)}";
    }

    private static string DescribeObject(IGH_DocumentObject obj)
    {
        var typeName = obj.GetType().Name;
        var nickname = obj.NickName ?? string.Empty;
        // Nickname is often the actual semantic name the user wrote; show both.
        return string.IsNullOrEmpty(nickname) || nickname == typeName
            ? typeName
            : $"\"{nickname}\" ({typeName})";
    }

    private static bool IsGeneSource(IGH_DocumentObject obj)
    {
        return obj is GH_NumberSlider
            || obj.GetType().Name == "GalapagosGeneListObject";
    }

    private static string SummariseGeneSources(IList<IGH_DocumentObject> sources)
    {
        var sliderCount = sources.Count(s => s is GH_NumberSlider);
        var poolCount = sources.Count - sliderCount;
        var parts = new List<string>();
        if (sliderCount > 0) parts.Add($"{sliderCount} slider{(sliderCount == 1 ? "" : "s")}");
        if (poolCount > 0) parts.Add($"{poolCount} gene pool{(poolCount == 1 ? "" : "s")}");
        return string.Join(", ", parts);
    }

    private static string GetCategory(IGH_DocumentObject obj)
    {
        // GH categories are exposed via Category property on Component/Param.
        if (obj is IGH_Component c) return c.Category ?? "Uncategorised";
        if (obj is IGH_Param p) return p.Category ?? "Params";
        return "Other";
    }
}
