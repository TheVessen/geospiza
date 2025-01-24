using GeospizaCore.Core;
using GeospizaCore.Utils;
using Newtonsoft.Json;

namespace GeospizaCore.ParallelSpiza;

/// <summary>
/// Represents a base class for observing and serializing the state of an evolutionary population.
/// This class provides functionality to track generations and their inhabitants, with JSON serialization support.
/// <remarks>
///  Use <see cref="EvolutionObserver"/> for a fully implemented observer to be used in a local context.
/// </remarks>
/// </summary>
public class ObserverServerSnapshot
{
  public int CurrentGenerationIndex { get; set; }
  public List<Individual> Inhabitants { get; set; }
  public int Count => Inhabitants.Count;
  public string RequestId { get; private set; }

  public ObserverServerSnapshot(EvolutionObserver observer)
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

  public static ObserverServerSnapshot? FromJson(string json)
  {
    var settings = new JsonSerializerSettings
    {
      Converters = new List<JsonConverter> { new Individual.IndividualConverter() },
      ContractResolver = new PrivateSetterContractResolver()
    };

    return JsonConvert.DeserializeObject<ObserverServerSnapshot>(json, settings);
  }
}