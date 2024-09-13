using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GeospizaManager.Core
{
    public class Gene
    {
        
       
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

        public Gene(Gene gene)
        {
            TickValue = gene.TickValue;
            GeneGuid = gene.GeneGuid;
            TickCount = gene.TickCount;
            GeneName = gene.GeneName;
            GhInstanceGuid = gene.GhInstanceGuid;
            GenePoolIndex = gene.GenePoolIndex;
        }

        public int TickValue { get; private set; }
        public Guid GeneGuid { get; }
        public int TickCount { get; }
        public string GeneName { get; }
        public Guid GhInstanceGuid { get; set; }
        public int GenePoolIndex { get; set; }

        // This function should only be used from a mutation strategy
        public void MutatedValue(int mutation)
        {
            TickValue = mutation;
        }
        
        public class GeneConverter : JsonConverter<Gene>
        {
            public override void WriteJson(JsonWriter writer, Gene value, JsonSerializer serializer)
            {
                writer.WriteRawValue(value.ToJson());
            }

            public override Gene ReadJson(JsonReader reader, Type objectType, Gene existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var jsonObject = JObject.Load(reader);
                return Gene.FromJson(jsonObject.ToString());
            }
        }
        
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

        public static Gene FromJson(string json)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver()
            };

            return JsonConvert.DeserializeObject<Gene>(json, settings);
        }

        public override int GetHashCode()
        {
            var hash = 17;

            hash = hash * 31 + GeneGuid.GetHashCode();
            hash = hash * 31 + TickValue.GetHashCode();

            return hash;
        }
    }
}