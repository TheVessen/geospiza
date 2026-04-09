using Newtonsoft.Json;

namespace GeospizaCore.Core;

/// <summary>
///     Immutable metadata for a gene that is identical across all individuals in a population.
///     Stored once per observer rather than repeated inside every Individual's GenePool.
/// </summary>
public class GeneSchema
{
    [JsonConstructor]
    public GeneSchema(Guid geneGuid, int tickCount, string geneName, Guid ghInstanceGuid, int genePoolIndex,
        double minValue = double.NaN, double maxValue = double.NaN)
    {
        GeneGuid = geneGuid;
        TickCount = tickCount;
        GeneName = geneName;
        GhInstanceGuid = ghInstanceGuid;
        GenePoolIndex = genePoolIndex;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public Guid GeneGuid { get; }
    public int TickCount { get; }
    public string GeneName { get; }
    public Guid GhInstanceGuid { get; }
    public int GenePoolIndex { get; }

    /// <summary>Real slider minimum (NaN if not available).</summary>
    public double MinValue { get; }

    /// <summary>Real slider maximum (NaN if not available).</summary>
    public double MaxValue { get; }

    /// <summary>Converts a tick index to the real slider value. Returns NaN if min/max are not set.</summary>
    public double TickToValue(int tick) =>
        double.IsNaN(MinValue) || double.IsNaN(MaxValue) || TickCount <= 0
            ? double.NaN
            : MinValue + (MaxValue - MinValue) * tick / TickCount;
}