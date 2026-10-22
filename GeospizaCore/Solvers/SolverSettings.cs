using GeospizaCore.Strategies;
using GeospizaCore.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaCore.Solvers;

/// <summary>
///     Represents configuration settings for an evolutionary algorithm solver.
/// </summary>
public class SolverSettings
{
    private int _eliteSize;
    private int _maxGenerations;
    private int _populationSize;
    private ICrossoverStrategy _crossoverStrategy = null!;
    private IMutationStrategy _mutationStrategy = null!;
    public ISelectionStrategy SelectionStrategy { get; set; } = null!;

    public ICrossoverStrategy CrossoverStrategy
    {
        get => _crossoverStrategy;
        set
        {
            _crossoverStrategy = value;
            ConfiguredCrossoverRate = (value as CrossoverStrategy)?.InitialCrossoverRate ?? value?.CrossoverRate ?? 0;
        }
    }

    public IMutationStrategy MutationStrategy
    {
        get => _mutationStrategy;
        set
        {
            _mutationStrategy = value;
            ConfiguredMutationRate = (value as MutationStrategy)?.InitialMutationRate ?? value?.MutationRate ?? 0;
        }
    }

    public IPairingStrategy PairingStrategy { get; set; } = null!;
    public ITerminationStrategy TerminationStrategy { get; set; } = null!;

    /// <summary>
    ///     The mutation rate as originally configured by the user (read from <see cref="MutationStrategy.InitialMutationRate"/>).
    ///     Always reflects the construction-time value, immune to drift from <c>AdaptStrategies</c>.
    /// </summary>
    public double ConfiguredMutationRate { get; private set; }

    /// <summary>
    ///     The crossover rate as originally configured by the user (read from <see cref="CrossoverStrategy.InitialCrossoverRate"/>).
    ///     Always reflects the construction-time value, immune to drift from <c>AdaptStrategies</c>.
    /// </summary>
    public double ConfiguredCrossoverRate { get; private set; }

    public int PopulationSize
    {
        get => _populationSize;
        set => _populationSize = value > 0
            ? value
            : throw new ArgumentException("Population size must be greater than 0");
    }

    public int MaxGenerations
    {
        get => _maxGenerations;
        set => _maxGenerations = value > 0
            ? value
            : throw new ArgumentException("Max generations must be greater than 0");
    }

    public int EliteSize
    {
        get => _eliteSize;
        set => _eliteSize = value >= 0 && value <= PopulationSize
            ? value
            : throw new ArgumentException("Elite size must be between 0 and population size");
    }

    /// <summary>
    ///     True when built from <c>GH_MultiObjectiveSettings</c>; false for single-objective.
    ///     Solvers use this to reject mismatched settings at run time.
    /// </summary>
    public bool IsMultiObjective { get; set; }

    /// <summary>
    ///     NSGA-III only: number of divisions used to generate Das &amp; Dennis reference points.
    ///     0 means not applicable (single-objective or NSGA-II).
    /// </summary>
    public int ReferencePointDivisions { get; set; }

    /// <summary>
    ///     When <c>true</c> (the default), the solver caches fitness results keyed on the
    ///     genotype's tick-value sequence so that duplicate individuals (common late in a run
    ///     when diversity collapses) skip the expensive Grasshopper solve. Set to <c>false</c>
    ///     when the fitness function is stochastic — caching would otherwise mask the
    ///     per-evaluation variance the algorithm needs to see.
    /// </summary>
    public bool UseFitnessCache { get; set; } = true;

    /// <summary>
    ///     Validates that all required strategies are properly set.
    ///     For multi-objective runs, SelectionStrategy is not required — NSGA solvers
    ///     use their own internal tournament selection based on Pareto rank and crowding distance.
    /// </summary>
    public void Validate()
    {
        if (!IsMultiObjective && SelectionStrategy == null)
            throw new InvalidOperationException("Selection strategy is required");
        if (CrossoverStrategy == null) throw new InvalidOperationException("Crossover strategy is required");
        if (MutationStrategy == null) throw new InvalidOperationException("Mutation strategy is required");
        if (PairingStrategy == null) throw new InvalidOperationException("Pairing strategy is required");
        if (TerminationStrategy == null) throw new InvalidOperationException("Termination strategy is required");
    }

    /// <summary>
    ///     Serializes the settings to a JSON string.
    /// </summary>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented, new EvoSettingsConverter());
    }

    /// <summary>
    ///     Deserializes settings from a JSON string.
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
///     Handles JSON conversion for SolverSettings, preserving type information for strategy implementations.
/// </summary>
public class EvoSettingsConverter : JsonConverter
{
    // Private setters must be writable so InitialMutationRate/InitialCrossoverRate (the user-configured
    // baselines behind ConfiguredMutationRate/ConfiguredCrossoverRate) survive a round-trip.
    private static readonly JsonSerializer _strategySerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        ContractResolver = new PrivateSetterContractResolver()
    });

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(SolverSettings);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not SolverSettings settings)
            throw new ArgumentException("Value must be of type SolverSettings", nameof(value));

        writer.WriteStartObject();

        WriteStrategy(writer, nameof(settings.SelectionStrategy), settings.SelectionStrategy);
        WriteStrategy(writer, nameof(settings.CrossoverStrategy), settings.CrossoverStrategy);
        WriteStrategy(writer, nameof(settings.MutationStrategy), settings.MutationStrategy);
        WriteStrategy(writer, nameof(settings.PairingStrategy), settings.PairingStrategy);
        WriteStrategy(writer, nameof(settings.TerminationStrategy), settings.TerminationStrategy);

        writer.WritePropertyName(nameof(settings.PopulationSize));
        writer.WriteValue(settings.PopulationSize);
        writer.WritePropertyName(nameof(settings.MaxGenerations));
        writer.WriteValue(settings.MaxGenerations);
        writer.WritePropertyName(nameof(settings.EliteSize));
        writer.WriteValue(settings.EliteSize);
        writer.WritePropertyName(nameof(settings.IsMultiObjective));
        writer.WriteValue(settings.IsMultiObjective);
        writer.WritePropertyName(nameof(settings.ReferencePointDivisions));
        writer.WriteValue(settings.ReferencePointDivisions);
        writer.WritePropertyName(nameof(settings.UseFitnessCache));
        writer.WriteValue(settings.UseFitnessCache);
        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        // Absent fields keep the SolverSettings defaults so JSON from older versions still loads.
        var settings = new SolverSettings
        {
            SelectionStrategy = GetStrategy<ISelectionStrategy>(jObject, nameof(SolverSettings.SelectionStrategy)),
            CrossoverStrategy = GetStrategy<ICrossoverStrategy>(jObject, nameof(SolverSettings.CrossoverStrategy)),
            MutationStrategy = GetStrategy<IMutationStrategy>(jObject, nameof(SolverSettings.MutationStrategy)),
            PairingStrategy = GetStrategy<IPairingStrategy>(jObject, nameof(SolverSettings.PairingStrategy)),
            TerminationStrategy =
                GetStrategy<ITerminationStrategy>(jObject, nameof(SolverSettings.TerminationStrategy))
        };

        // PopulationSize before EliteSize: the EliteSize setter validates against it.
        if (TryGetValue<int>(jObject, nameof(SolverSettings.PopulationSize), out var populationSize))
            settings.PopulationSize = populationSize;
        if (TryGetValue<int>(jObject, nameof(SolverSettings.MaxGenerations), out var maxGenerations))
            settings.MaxGenerations = maxGenerations;
        if (TryGetValue<int>(jObject, nameof(SolverSettings.EliteSize), out var eliteSize))
            settings.EliteSize = eliteSize;
        if (TryGetValue<bool>(jObject, nameof(SolverSettings.IsMultiObjective), out var isMultiObjective))
            settings.IsMultiObjective = isMultiObjective;
        if (TryGetValue<int>(jObject, nameof(SolverSettings.ReferencePointDivisions), out var divisions))
            settings.ReferencePointDivisions = divisions;
        if (TryGetValue<bool>(jObject, nameof(SolverSettings.UseFitnessCache), out var useFitnessCache))
            settings.UseFitnessCache = useFitnessCache;

        settings.Validate();
        return settings;
    }

    private static void WriteStrategy(JsonWriter writer, string propertyName, object? strategy)
    {
        writer.WritePropertyName(propertyName);
        WriteTyped(writer, strategy, _strategySerializer);
    }

    private static T GetStrategy<T>(JObject jObject, string strategyName) where T : class
    {
        return ReadTyped<T>(jObject[strategyName], _strategySerializer)!;
    }

    private static bool TryGetValue<T>(JObject jObject, string name, out T value)
    {
        var token = jObject[name];
        if (token == null || token.Type == JTokenType.Null)
        {
            value = default!;
            return false;
        }

        value = token.Value<T>()!;
        return true;
    }

    /// <summary>
    ///     Writes <paramref name="value" /> as a <c>{ "Type": assembly-qualified name, "Value": ... }</c> envelope,
    ///     or as JSON null when it is null.
    /// </summary>
    internal static void WriteTyped(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("Type");
        writer.WriteValue(value.GetType().AssemblyQualifiedName);
        writer.WritePropertyName("Value");
        serializer.Serialize(writer, value);
        writer.WriteEndObject();
    }

    /// <summary>
    ///     Reads an envelope written by <see cref="WriteTyped" />; a missing or null token yields null.
    /// </summary>
    internal static T? ReadTyped<T>(JToken? token, JsonSerializer serializer) where T : class
    {
        if (token == null || token.Type == JTokenType.Null) return null;

        var typeName = token["Type"]?.Value<string>();
        var type = Type.GetType(typeName ?? string.Empty) ??
                   throw new TypeLoadException($"Cannot find type '{typeName}'");

        return (T?)token["Value"]?.ToObject(type, serializer);
    }
}
