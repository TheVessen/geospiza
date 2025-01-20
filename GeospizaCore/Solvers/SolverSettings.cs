using GeospizaManager.Strategies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace GeospizaManager.Solvers;

/// <summary>
/// Represents configuration settings for an evolutionary algorithm solver.
/// </summary>
public class SolverSettings
{
  private int populationSize;
  private int maxGenerations;
  private int eliteSize;
  public ISelectionStrategy SelectionStrategy { get; set; } = null!;
  public ICrossoverStrategy CrossoverStrategy { get; set; } = null!;
  public IMutationStrategy MutationStrategy { get; set; } = null!;
  public PairingStrategy PairingStrategy { get; set; } = null!;
  public ITerminationStrategy TerminationStrategy { get; set; } = null!;

  public int PopulationSize
  {
    get => populationSize;
    set => populationSize = value > 0
      ? value
      : throw new ArgumentException("Population size must be greater than 0");
  }

  public int MaxGenerations
  {
    get => maxGenerations;
    set => maxGenerations = value > 0
      ? value
      : throw new ArgumentException("Max generations must be greater than 0");
  }

  public int EliteSize
  {
    get => eliteSize;
    set => eliteSize = value >= 0 && value <= PopulationSize
      ? value
      : throw new ArgumentException("Elite size must be between 0 and population size");
  }

  /// <summary>
  /// Validates that all required strategies are properly set.
  /// </summary>
  public void Validate()
  {
    if (SelectionStrategy == null) throw new InvalidOperationException("Selection strategy is required");
    if (CrossoverStrategy == null) throw new InvalidOperationException("Crossover strategy is required");
    if (MutationStrategy == null) throw new InvalidOperationException("Mutation strategy is required");
    if (PairingStrategy == null) throw new InvalidOperationException("Pairing strategy is required");
    if (TerminationStrategy == null) throw new InvalidOperationException("Termination strategy is required");
  }

  /// <summary>
  /// Serializes the settings to a JSON string.
  /// </summary>
  public string ToJson()
  {
    return JsonConvert.SerializeObject(this, Formatting.Indented, new EvoSettingsConverter());
  }

  /// <summary>
  /// Deserializes settings from a JSON string.
  /// </summary>
  public static SolverSettings FromJson(string json)
  {
    if (string.IsNullOrWhiteSpace(json))
      throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

    var settings = JsonConvert.DeserializeObject<SolverSettings>(
      json,
      new EvoSettingsConverter());

    if (settings == null)
      throw new JsonSerializationException("Failed to deserialize solver settings");

    settings.Validate();
    return settings;
  }
}

/// <summary>
/// Handles JSON conversion for SolverSettings, preserving type information for strategy implementations.
/// </summary>
public class EvoSettingsConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(SolverSettings);
  }

  public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  {
    if (value is not SolverSettings settings)
      throw new ArgumentException("Value must be of type SolverSettings", nameof(value));

    var strategyProperties = new Dictionary<string, object>
    {
      { nameof(settings.SelectionStrategy), settings.SelectionStrategy },
      { nameof(settings.CrossoverStrategy), settings.CrossoverStrategy },
      { nameof(settings.MutationStrategy), settings.MutationStrategy },
      { nameof(settings.PairingStrategy), settings.PairingStrategy },
      { nameof(settings.TerminationStrategy), settings.TerminationStrategy }
    };

    writer.WriteStartObject();

    foreach (var kvp in strategyProperties)
    {
      var propertyName = kvp.Key;
      var strategy = kvp.Value;
      WriteStrategy(writer, propertyName, strategy, serializer);
    }

    writer.WritePropertyName(nameof(settings.PopulationSize));
    writer.WriteValue(settings.PopulationSize);
    writer.WritePropertyName(nameof(settings.MaxGenerations));
    writer.WriteValue(settings.MaxGenerations);
    writer.WritePropertyName(nameof(settings.EliteSize));
    writer.WriteValue(settings.EliteSize);
    writer.WriteEndObject();
  }

  public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
  {
    var jObject = JObject.Load(reader);
    var settings = new SolverSettings
    {
      SelectionStrategy =
        GetStrategy<ISelectionStrategy>(jObject, nameof(SolverSettings.SelectionStrategy), serializer),
      CrossoverStrategy =
        GetStrategy<ICrossoverStrategy>(jObject, nameof(SolverSettings.CrossoverStrategy), serializer),
      MutationStrategy = GetStrategy<IMutationStrategy>(jObject, nameof(SolverSettings.MutationStrategy), serializer),
      PairingStrategy = GetStrategy<PairingStrategy>(jObject, nameof(SolverSettings.PairingStrategy), serializer),
      TerminationStrategy =
        GetStrategy<ITerminationStrategy>(jObject, nameof(SolverSettings.TerminationStrategy), serializer),
      PopulationSize = jObject[nameof(SolverSettings.PopulationSize)]!.Value<int>(),
      MaxGenerations = jObject[nameof(SolverSettings.MaxGenerations)]!.Value<int>(),
      EliteSize = jObject[nameof(SolverSettings.EliteSize)]!.Value<int>()
    };

    settings.Validate();
    return settings;
  }

  private static void WriteStrategy(JsonWriter writer, string propertyName, object strategy, JsonSerializer serializer)
  {
    writer.WritePropertyName(propertyName);
    writer.WriteStartObject();
    writer.WritePropertyName("Type");
    writer.WriteValue(strategy.GetType().AssemblyQualifiedName);
    writer.WritePropertyName("Value");
    serializer.Serialize(writer, strategy);
    writer.WriteEndObject();
  }

  private static T GetStrategy<T>(JObject jObject, string strategyName, JsonSerializer serializer)
  {
    var strategyJObject = (JObject)jObject[strategyName]!;
    var strategyType = strategyJObject["Type"]!.Value<string>();
    var type = Type.GetType(strategyType!) ??
               throw new TypeLoadException($"Cannot find type '{strategyType}'");

    return (T)strategyJObject["Value"]!.ToObject(type, serializer)!;
  }
}