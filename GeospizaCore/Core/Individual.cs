using GeospizaManager.Utils;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaManager.Core;

public class Individual : IEquatable<Individual>
{
  // Properties with private setters for better encapsulation
  public IReadOnlyList<Gene> GenePool { get; }
  public double Fitness { get; private set; }
  public double Probability { get; private set; }
  public int Generation { get; private set; }

  // Keep private list for internal modifications
  private readonly List<Gene> _genePool;

  public Individual()
  {
    _genePool = new List<Gene>();
    GenePool = _genePool.AsReadOnly();
  }

  public Individual(IEnumerable<Gene> genePool)
  {
    _genePool = new List<Gene>(genePool ?? throw new ArgumentNullException(nameof(genePool)));
    GenePool = _genePool.AsReadOnly();
  }

  public Individual(Individual individual)
  {
    if (individual == null) throw new ArgumentNullException(nameof(individual));

    _genePool = new List<Gene>(individual.GenePool);
    GenePool = _genePool.AsReadOnly();
    Fitness = individual.Fitness;
    Probability = 0;
    Generation = individual.Generation;
  }

  public Individual(string json)
  {
    var parsed = FromJson(json) ?? throw new ArgumentException("Failed to parse individual from JSON.", nameof(json));

    _genePool = new List<Gene>(parsed.GenePool);
    GenePool = _genePool.AsReadOnly();
    Fitness = parsed.Fitness;
    Probability = parsed.Probability;
    Generation = parsed.Generation;
  }

  public void AddStableGene(Gene gene)
  {
    if (gene == null) throw new ArgumentNullException(nameof(gene));
    _genePool.Add(gene);
  }

  public void SetFitness(double fitness)
  {
    if (double.IsNaN(fitness)) throw new ArgumentException("Fitness cannot be NaN", nameof(fitness));
    if (double.IsInfinity(fitness)) throw new ArgumentException("Fitness cannot be infinity", nameof(fitness));

    Fitness = fitness;
  }

  public void SetProbability(double normalizedFitness)
  {
    if (normalizedFitness < 0 || normalizedFitness > 1)
      throw new ArgumentException("Normalized fitness must be between 0 and 1", nameof(normalizedFitness));

    Probability = normalizedFitness;
  }

  public void SetGeneration(int generation)
  {
    if (generation < 0) throw new ArgumentException("Generation cannot be negative", nameof(generation));
    Generation = generation;
  }

  public void Reinstate(StateManager stateManager)
  {
    if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));

    foreach (var gene in GenePool)
    {
      var matchingGene = stateManager.Genotype.GetValueOrDefault(gene.GeneGuid);
      matchingGene?.SetTickValue(gene.TickValue, stateManager);
    }
  }

  public void Reinstate(GH_Document doc)
  {
    if (doc == null) throw new ArgumentNullException(nameof(doc));

    foreach (var gene in GenePool)
    {
      var slider = doc.FindObject(gene.GhInstanceGuid, true)
                   ?? throw new InvalidOperationException(
                     $"Gene with GUID {gene.GhInstanceGuid} not found in document.");

      if (slider is GH_NumberSlider numberSlider)
      {
        numberSlider.TickValue = gene.TickValue;
      }
      else if (slider.GetType().ToString() == "GalapagosComponents.GalapagosGeneListObject")
      {
        dynamic genePool = slider;
        genePool.set_TickValue(gene.GenePoolIndex, gene.TickValue);
      }
      else
      {
        throw new InvalidOperationException($"Unsupported gene type: {slider.GetType()}");
      }
    }
  }

  public override bool Equals(object? obj)
  {
    if (obj is Individual other)
      return Equals(other);
    return false;
  }

  public bool Equals(Individual? other)
  {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;

    return GenePool.SequenceEqual(other.GenePool) &&
           Math.Abs(Fitness - other.Fitness) < double.Epsilon;
  }

  public override int GetHashCode()
  {
    unchecked
    {
      var hash = 17;
      foreach (var gene in GenePool) hash = hash * 31 + (gene?.GetHashCode() ?? 0);
      hash = hash * 31 + Fitness.GetHashCode();
      return hash;
    }
  }

  public string ToJson()
  {
    var settings = new JsonSerializerSettings
    {
      Formatting = Formatting.Indented,
      Converters = new List<JsonConverter> { new Gene.GeneConverter() }
    };

    return JsonConvert.SerializeObject(this, settings);
  }

  public static Individual FromJson(string json)
  {
    if (string.IsNullOrEmpty(json))
      throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

    var settings = new JsonSerializerSettings
    {
      ContractResolver = new PrivateSetterContractResolver(),
      Converters = new List<JsonConverter> { new Gene.GeneConverter() }
    };

    return JsonConvert.DeserializeObject<Individual>(json, settings)
           ?? throw new JsonSerializationException("Failed to deserialize Individual from JSON");
  }

  public class IndividualConverter : JsonConverter<Individual>
  {
    public override void WriteJson(JsonWriter writer, Individual value, JsonSerializer serializer)
    {
      if (writer == null) throw new ArgumentNullException(nameof(writer));
      if (value == null) throw new ArgumentNullException(nameof(value));

      writer.WriteRawValue(value.ToJson());
    }

    public override Individual ReadJson(JsonReader reader, Type objectType, Individual existingValue,
      bool hasExistingValue, JsonSerializer serializer)
    {
      if (reader == null) throw new ArgumentNullException(nameof(reader));

      var jsonObject = JObject.Load(reader);
      return FromJson(jsonObject.ToString());
    }
  }
}