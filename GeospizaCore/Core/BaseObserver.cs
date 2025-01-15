using GeospizaManager.Utils;
using Newtonsoft.Json;

namespace GeospizaManager.Core;

public class BaseObserver
{
  public int CurrentGenerationIndex { get; set; }

  public List<Individual> Inhabitants { get; set; } = new();
  public int Count => Inhabitants.Count;
  public string RequestId { get; private set; }

  public BaseObserver(EvolutionObserver observer)
  {
    CurrentGenerationIndex = observer.CurrentGenerationIndex;
    Inhabitants = observer.CurrentPopulation.Inhabitants;
    RequestId = new Guid().ToString();
  }

  public string ToJson()
  {
    var settings = new JsonSerializerSettings
    {
      Formatting = Formatting.Indented,
      Converters = new List<JsonConverter> { new Individual.IndividualConverter() }
    };

    return JsonConvert.SerializeObject(this, settings);
  }

  public static BaseObserver? FromJson(string json)
  {
    var settings = new JsonSerializerSettings
    {
      Converters = new List<JsonConverter> { new Individual.IndividualConverter() },
      ContractResolver = new PrivateSetterContractResolver()
    };

    return JsonConvert.DeserializeObject<BaseObserver>(json, settings);
  }
}