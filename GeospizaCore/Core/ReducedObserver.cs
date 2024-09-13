using Newtonsoft.Json;

namespace GeospizaManager.Core;

public class ReducedObserver
{
    public int CurrentGenerationIndex { get;  set; }

    public List<Individual> Inhabitants { get;  set; } = new();
    
    public int Count => Inhabitants.Count;
    public string RequestId { get; private set; }
    
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Individual.IndividualConverter() }
        };

        return JsonConvert.SerializeObject(this, settings);
    }

    public static ReducedObserver? FromJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new Individual.IndividualConverter() },
            ContractResolver = new PrivateSetterContractResolver()
        };

        return JsonConvert.DeserializeObject<ReducedObserver>(json, settings);
    }
}