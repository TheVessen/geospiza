using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Grasshopper.Kernel;

namespace GeospizaPlugin.AI;

/// <summary>
///     Computes a stable hash of a Grasshopper canvas's topology so cached canvas summaries
///     can be invalidated only when the user actually rewires the graph. Slider values,
///     positions, colors, and other cosmetic state are intentionally excluded — those change
///     constantly during normal use and should not invalidate the summary.
/// </summary>
public static class CanvasFingerprint
{
    /// <summary>
    ///     Returns a hex SHA-256 digest of the canvas's topology: each object's type,
    ///     nickname, and the InstanceGuids of its input sources. Objects are sorted by their
    ///     own InstanceGuid so the result is deterministic regardless of canvas-object order.
    /// </summary>
    public static string Compute(GH_Document doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        var entries = new List<string>(doc.Objects.Count);
        foreach (var obj in doc.Objects.OrderBy(o => o.InstanceGuid))
        {
            // Type + nickname captures intent ("MyVoronoi" rebrand of a generic node).
            // Inputs capture wiring; everything else is cosmetic.
            var typeName = obj.GetType().FullName ?? "?";
            var nickname = obj.NickName ?? string.Empty;
            var inputGuids = CollectInputSourceGuids(obj);

            entries.Add($"{typeName}|{nickname}|{string.Join(",", inputGuids)}");
        }

        return Sha256Hex(string.Join("\n", entries));
    }

    private static IEnumerable<string> CollectInputSourceGuids(IGH_DocumentObject obj)
    {
        // Components have multiple input params, each with multiple sources.
        if (obj is IGH_Component component)
        {
            return component.Params.Input
                .SelectMany(p => p.Sources)
                .Select(s => s.InstanceGuid.ToString())
                .OrderBy(s => s, StringComparer.Ordinal);
        }

        // Standalone params (like a relay) have their own Sources collection.
        if (obj is IGH_Param param)
        {
            return param.Sources
                .Select(s => s.InstanceGuid.ToString())
                .OrderBy(s => s, StringComparer.Ordinal);
        }

        return Array.Empty<string>();
    }

    private static string Sha256Hex(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
