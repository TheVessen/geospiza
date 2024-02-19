using System;
using Geospiza.Strategies;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Gene
{   
    public int TickValue { get; private set; }
    public Guid GeneGuid { get; private set; }
    public int TickCount { get; private set; }
    public string GeneName { get; private set; }
    public Guid GhInstanceGuid { get; init; }
    public int GenePoolIndex { get; init; }
    
    public Gene(int tickValue, Guid geneGuid, int tickCount, string geneName, Guid ghInstanceGuid, int genePoolIndex)
        {
            TickValue = tickValue;
            GeneGuid = geneGuid;
            TickCount = tickCount;
            GeneName = geneName;
            GhInstanceGuid = ghInstanceGuid;
            GenePoolIndex = genePoolIndex;
        }
    
    public Gene(Gene gene)
    {
        TickValue = gene.TickValue;
        GeneGuid = gene.GeneGuid;
        TickCount = gene.TickCount;
        GeneName = gene.GeneName;
        GhInstanceGuid = gene.GhInstanceGuid;
        GenePoolIndex = gene.GenePoolIndex;
    }
    
    // This function should only be used from a mutation strategy
    public void MutatedValue(int mutation)
    {
        TickValue = mutation;
    }
    
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                IgnoreSerializableInterface = true,
                IgnoreSerializableAttribute = true
            }
        };

        var obj = new
        {
            GeneGuid = GeneGuid,
            TickValue = TickValue,
            TickCount = TickCount,
            GeneName = GeneName,
            GhInstanceGuid = GhInstanceGuid,
            GenePoolIndex = GenePoolIndex
        };

        return JsonConvert.SerializeObject(obj, settings);
    }
    
}