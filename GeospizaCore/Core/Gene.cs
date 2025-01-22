using GeospizaManager.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GeospizaManager.Core;

/// <summary>
/// Represents a Gene with various properties and methods for JSON serialization.
/// </summary>
public class Gene
{
  /// <summary>
  /// Initializes a new instance of the <see cref="Gene"/> class with specified values.
  /// </summary>
  /// <param name="tickValue">The tick value of the gene.</param>
  /// <param name="geneGuid">The unique identifier for the gene.</param>
  /// <param name="tickCount">The tick count of the gene.</param>
  /// <param name="geneName">The name of the gene.</param>
  /// <param name="ghInstanceGuid">The unique identifier for the GH instance.</param>
  /// <param name="genePoolIndex">The index of the gene in the gene pool.</param>
  [JsonConstructor]
  public Gene(int tickValue, Guid geneGuid, int tickCount, string geneName, Guid ghInstanceGuid, int genePoolIndex)
  {
    TickValue = tickValue;
    GeneGuid = geneGuid;
    TickCount = tickCount;
    GeneName = geneName;
    GhInstanceGuid = ghInstanceGuid;
    GenePoolIndex = genePoolIndex;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Gene"/> class by copying another gene.
  /// </summary>
  /// <param name="gene">The gene to copy.</param>
  public Gene(Gene gene)
  {
    TickValue = gene.TickValue;
    GeneGuid = gene.GeneGuid;
    TickCount = gene.TickCount;
    GeneName = gene.GeneName;
    GhInstanceGuid = gene.GhInstanceGuid;
    GenePoolIndex = gene.GenePoolIndex;
  }

  /// <summary>
  /// Gets the tick value of the gene. <see cref="GeneTemplate"/> For a gene pool list, this value represents the position on the slider.
  /// <remarks>
  /// </remarks>
  /// </summary>
  public int TickValue { get; private set; }

  public Guid GeneGuid { get; }

  /// <summary>
  /// Gets the tick count of the gene.
  /// </summary>
  /// <remarks>
  /// The tick count refers to a Rhino gene-pool slider defined in <see cref="GeneTemplate"/>.
  /// It describes the total number of positions on the slider. The <see cref="TickValue"/> represents the position on the slider, which in turn returns the actual slider value.
  /// </remarks>
  public int TickCount { get; }

  public string GeneName { get; }

  /// <summary>
  /// Gets or sets the unique identifier for the GH instance.
  /// </summary>
  public Guid GhInstanceGuid { get; set; }

  /// <summary>
  /// Gets or sets the index of the gene in the gene pool.
  /// </summary>
  public int GenePoolIndex { get; set; }

  /// <summary>
  /// Sets the tick value of the gene. This function should only be used from a mutation strategy.
  /// </summary>
  /// <param name="mutation">The new tick value after mutation.</param>
  public void MutatedValue(int mutation)
  {
    TickValue = mutation;
  }

  /// <summary>
  /// Custom JSON converter for the <see cref="Gene"/> class.
  /// </summary>
  public class GeneConverter : JsonConverter<Gene>
  {
    /// <summary>
    /// Writes the JSON representation of the <see cref="Gene"/> object.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The gene object to write.</param>
    /// <param name="serializer">The JSON serializer.</param>
    public override void WriteJson(JsonWriter writer, Gene value, JsonSerializer serializer)
    {
      writer.WriteRawValue(value.ToJson());
    }

    /// <summary>
    /// Reads the JSON representation of the <see cref="Gene"/> object.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The type of the object.</param>
    /// <param name="existingValue">The existing value of the object being read.</param>
    /// <param name="hasExistingValue">Whether the object has an existing value.</param>
    /// <param name="serializer">The JSON serializer.</param>
    /// <returns>The deserialized gene object.</returns>
    public override Gene? ReadJson(JsonReader reader, Type objectType, Gene existingValue, bool hasExistingValue,
      JsonSerializer serializer)
    {
      var jsonObject = JObject.Load(reader);
      return FromJson(jsonObject.ToString());
    }
  }

  /// <summary>
  /// Converts the gene object to its JSON representation.
  /// </summary>
  /// <returns>The JSON string representation of the gene.</returns>
  public string ToJson()
  {
    var settings = new JsonSerializerSettings
    {
      ContractResolver = new DefaultContractResolver
      {
        IgnoreSerializableInterface = true,
        IgnoreSerializableAttribute = true
      },
      NullValueHandling = NullValueHandling.Ignore
    };

    return JsonConvert.SerializeObject(this, settings);
  }

  public override bool Equals(object? obj)
  {
    if (obj is not Gene other) return false;
    return GeneGuid == other.GeneGuid && TickValue == other.TickValue;
  }

  /// <summary>
  /// Creates a gene object from its JSON representation.
  /// </summary>
  /// <param name="json">The JSON string representation of the gene.</param>
  /// <returns>The deserialized gene object.</returns>
  public static Gene? FromJson(string json)
  {
    if (string.IsNullOrEmpty(json))
      throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

    var settings = new JsonSerializerSettings
    {
      ContractResolver = new PrivateSetterContractResolver()
    };

    try
    {
      return JsonConvert.DeserializeObject<Gene>(json, settings);
    }
    catch (JsonException ex)
    {
      throw new ArgumentException("Invalid JSON format", nameof(json), ex);
    }
  }

  /// <summary>
  /// Gets the hash code for the gene object.
  /// </summary>
  /// <returns>The hash code of the gene object.</returns>
  public override int GetHashCode()
  {
    var hash = 17;

    hash = hash * 31 + GeneGuid.GetHashCode();
    hash = hash * 31 + TickValue.GetHashCode();

    return hash;
  }
}