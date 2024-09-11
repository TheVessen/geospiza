using GeospizaManager.Strategies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaManager.Solvers;

public class EvolutionaryAlgorithmSettings
{
    // Constructor to initialize default values

    public ISelectionStrategy SelectionStrategy { get; set; }
    public ICrossoverStrategy CrossoverStrategy { get; set; }
    public IMutationStrategy MutationStrategy { get; set; }
    public PairingStrategy PairingStrategy { get; set; }
    public ITerminationStrategy TerminationStrategy { get; set; }
    public int PopulationSize { get; set; }
    public int MaxGenerations { get; set; }
    public int EliteSize { get; set; }


    public class EvolutionaryAlgorithmSettingsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(EvolutionaryAlgorithmSettings));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            EvolutionaryAlgorithmSettings settings = (EvolutionaryAlgorithmSettings)value;

            writer.WriteStartObject();
            writer.WritePropertyName("SelectionStrategy");
            writer.WriteStartObject();
            writer.WritePropertyName("Type");
            writer.WriteValue(settings.SelectionStrategy.GetType().FullName);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, settings.SelectionStrategy);
            writer.WriteEndObject();

            // Do the same for other strategies
            writer.WritePropertyName("CrossoverStrategy");
            writer.WriteStartObject();
            writer.WritePropertyName("Type");
            writer.WriteValue(settings.CrossoverStrategy.GetType().FullName);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, settings.CrossoverStrategy);
            writer.WriteEndObject();

            writer.WritePropertyName("MutationStrategy");
            writer.WriteStartObject();
            writer.WritePropertyName("Type");
            writer.WriteValue(settings.MutationStrategy.GetType().FullName);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, settings.MutationStrategy);
            writer.WriteEndObject();

            writer.WritePropertyName("PairingStrategy");
            writer.WriteStartObject();
            writer.WritePropertyName("Type");
            writer.WriteValue(settings.PairingStrategy.GetType().FullName);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, settings.PairingStrategy);
            writer.WriteEndObject();

            writer.WritePropertyName("TerminationStrategy");
            writer.WriteStartObject();
            writer.WritePropertyName("Type");
            writer.WriteValue(settings.TerminationStrategy.GetType().FullName);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, settings.TerminationStrategy);
            writer.WriteEndObject();

            writer.WritePropertyName("PopulationSize");
            writer.WriteValue(settings.PopulationSize);
            writer.WritePropertyName("MaxGenerations");
            writer.WriteValue(settings.MaxGenerations);
            writer.WritePropertyName("EliteSize");
            writer.WriteValue(settings.EliteSize);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            EvolutionaryAlgorithmSettings settings = new EvolutionaryAlgorithmSettings();

            // Add null checks for all strategies
            settings.SelectionStrategy = GetStrategy<ISelectionStrategy>(jObject, "SelectionStrategy", serializer);
            settings.CrossoverStrategy = GetStrategy<ICrossoverStrategy>(jObject, "CrossoverStrategy", serializer);
            settings.MutationStrategy = GetStrategy<IMutationStrategy>(jObject, "MutationStrategy", serializer);
            settings.PairingStrategy = GetStrategy<PairingStrategy>(jObject, "PairingStrategy", serializer);
            settings.TerminationStrategy =
                GetStrategy<ITerminationStrategy>(jObject, "TerminationStrategy", serializer);

            settings.PopulationSize = jObject["PopulationSize"].Value<int>();
            settings.MaxGenerations = jObject["MaxGenerations"].Value<int>();
            settings.EliteSize = jObject["EliteSize"].Value<int>();

            return settings;
        }

        private T GetStrategy<T>(JObject jObject, string strategyName, JsonSerializer serializer)
        {
            JObject strategyJObject = (JObject)jObject[strategyName];
            string strategyType = strategyJObject["Type"].Value<string>();
            Type strategyActualType = Type.GetType(strategyType);
            if (strategyActualType == null)
            {
                throw new Exception($"Cannot find type '{strategyType}'");
            }

            return (T)strategyJObject["Value"].ToObject(strategyActualType, serializer);
        }
    }


    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented, new EvolutionaryAlgorithmSettingsConverter());
    }

    public static EvolutionaryAlgorithmSettings FromJson(string json)
    {
        if(json =="") throw new Exception("Json string is empty");
        EvolutionaryAlgorithmSettings settings = JsonConvert.DeserializeObject<EvolutionaryAlgorithmSettings>(json, new EvolutionaryAlgorithmSettingsConverter());
        return settings;
    }
}