using Newtonsoft.Json;

namespace GeospizaCore.Core;

/// <summary>
///     Immutable metadata for a gene that is identical across all individuals in a population.
///     Stored once per observer rather than repeated inside every Individual's GenePool.
/// </summary>
public class GeneSchema
{
    [JsonConstructor]
    public GeneSchema(Guid geneGuid, int tickCount, string geneName, Guid ghInstanceGuid, int genePoolIndex)
    {
        GeneGuid = geneGuid;
        TickCount = tickCount;
        GeneName = geneName;
        GhInstanceGuid = ghInstanceGuid;
        GenePoolIndex = genePoolIndex;
    }

    public Guid GeneGuid { get; }
    public int TickCount { get; }
    public string GeneName { get; }
    public Guid GhInstanceGuid { get; }
    public int GenePoolIndex { get; }
}
