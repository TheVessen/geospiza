using GeospizaCore.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaCore.Core;

public class Individual : IEquatable<Individual>
{
    private static readonly JsonSerializerSettings _toJsonSettings = new()
    {
        FloatFormatHandling = FloatFormatHandling.String,
        Converters = { new Gene.GeneConverter() }
    };

    private static readonly JsonSerializerSettings _fromJsonSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver(),
        Converters = { new Gene.GeneConverter() }
    };

    // Keep private list for internal modifications
    private readonly List<Gene> _genePool;

    public Individual()
    {
        _genePool = new List<Gene>();
        GenePool = _genePool.AsReadOnly();
    }

    /// <summary>
    ///     Creates a new individual object from a gene pool.
    /// </summary>
    /// <param name="genePool"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public Individual(IEnumerable<Gene> genePool)
    {
        _genePool = (genePool ?? throw new ArgumentNullException(nameof(genePool)))
            .Select(g => new Gene(g)).ToList();
        GenePool = _genePool.AsReadOnly();
    }

    /// <summary>
    ///     Creates a new individual object from an existing individual.
    /// </summary>
    /// <param name="individual"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public Individual(Individual individual)
    {
        if (individual == null) throw new ArgumentNullException(nameof(individual));

        _genePool = individual.GenePool.Select(g => new Gene(g)).ToList();
        GenePool = _genePool.AsReadOnly();
        Fitness = individual.Fitness;
        Probability = individual.Probability;
        Generation = individual.Generation;
        Objectives = individual.Objectives != null ? (double[])individual.Objectives.Clone() : null;
        ParetoRank = individual.ParetoRank;
        CrowdingDistance = individual.CrowdingDistance;
        ReferencePointIndex = individual.ReferencePointIndex;
    }

    /// <summary>
    ///     Creates an individual object from its JSON representation.
    /// </summary>
    /// <param name="json"></param>
    /// <exception cref="ArgumentException"></exception>
    public Individual(string json)
    {
        var parsed = FromJson(json) ??
                     throw new ArgumentException("Failed to parse individual from JSON.", nameof(json));
        _genePool = new List<Gene>(parsed.GenePool);
        GenePool = _genePool.AsReadOnly();
        Fitness = parsed.Fitness;
        Probability = parsed.Probability;
        Generation = parsed.Generation;
    }

    public IReadOnlyList<Gene> GenePool { get; }

    /// <summary>
    ///     The fitness of the individual. This is a measure of how well the individual solves the problem.
    /// </summary>
    public double Fitness { get; private set; }

    /// <summary>
    ///     The probability of the individual to be selected for reproduction.
    /// </summary>
    public double Probability { get; private set; }

    /// <summary>
    ///     Multi-objective fitness values. Null when single-objective mode is used.
    /// </summary>
    public double[]? Objectives { get; private set; }

    /// <summary>
    ///     Pareto dominance rank (0 = Pareto-optimal front). Set by NSGA-II sort.
    /// </summary>
    public int ParetoRank { get; private set; }

    /// <summary>
    ///     Crowding distance within a Pareto front. Higher is better for diversity.
    /// </summary>
    public double CrowdingDistance { get; private set; }

    /// <summary>
    ///     Index of the nearest NSGA-III reference point. Transient — recomputed each generation, not serialized.
    /// </summary>
    public int ReferencePointIndex { get; private set; } = -1;

    public int Generation { get; private set; }

    public bool Equals(Individual? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return GenePool.SequenceEqual(other.GenePool) &&
               Math.Abs(Fitness - other.Fitness) < double.Epsilon;
    }

    /// <summary>
    ///     Adds a gene to the individual's gene pool.
    /// </summary>
    /// <param name="gene"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddGene(Gene gene)
    {
        if (gene == null) throw new ArgumentNullException(nameof(gene));
        _genePool.Add(gene);
    }

    /// <summary>
    ///     Sets the fitness of the individual.
    /// </summary>
    /// <param name="fitness"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetFitness(double fitness)
    {
        if (double.IsNaN(fitness)) throw new ArgumentException("Fitness cannot be NaN", nameof(fitness));
        if (double.IsInfinity(fitness)) throw new ArgumentException("Fitness cannot be infinity", nameof(fitness));

        Fitness = fitness;
    }

    /// <summary>
    ///     Sets the probability of the individual to be selected for reproduction.
    /// </summary>
    /// <param name="normalizedFitness"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetProbability(double normalizedFitness)
    {
        if (normalizedFitness < 0 || normalizedFitness > 1)
            throw new ArgumentException("Normalized fitness must be between 0 and 1", nameof(normalizedFitness));

        Probability = normalizedFitness;
    }

    public void SetObjectives(double[] objectives)
    {
        Objectives = objectives;
    }

    public void SetParetoRank(int rank)
    {
        ParetoRank = rank;
    }

    public void SetCrowdingDistance(double distance)
    {
        CrowdingDistance = distance;
    }

    public void SetReferencePointIndex(int index)
    {
        ReferencePointIndex = index;
    }

    /// <summary>
    ///     Sets the generation of the individual that its living in.
    /// </summary>
    /// <param name="generation"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetGeneration(int generation)
    {
        if (generation < 0) throw new ArgumentException("Generation cannot be negative", nameof(generation));
        Generation = generation;
    }

    /// <summary>
    ///     Reinstates the individual's gene pool to the state of the state manager. Meaning you see it in your GH document.
    /// </summary>
    /// <param name="stateManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Reinstate(StateManager stateManager)
    {
        if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));

        foreach (var gene in GenePool)
        {
            if (gene.GenePoolIndex >= 0)
            {
                if (stateManager.AllGenePools.TryGetValue(gene.GhInstanceGuid, out var genePool))
                    genePool.set_TickValue(gene.GenePoolIndex, gene.TickValue);
            }
            else
            {
                if (stateManager.AllSliders.TryGetValue(gene.GhInstanceGuid, out var slider))
                    slider.TickValue = gene.TickValue;
            }
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is Individual other)
            return Equals(other);
        return false;
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    ///     Returns a JSON representation of the individual.
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, _toJsonSettings);
    }

    /// <summary>
    ///     Creates an individual object from its JSON representation.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="JsonSerializationException"></exception>
    public static Individual FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        return JsonConvert.DeserializeObject<Individual>(json, _fromJsonSettings)
               ?? throw new JsonSerializationException("Failed to deserialize Individual from JSON");
    }

    /// <summary>
    ///     Custom JSON converter for the <see cref="Individual" /> class.
    /// </summary>
    public class IndividualConverter : JsonConverter<Individual>
    {
        // No IndividualConverter here to avoid infinite recursion when deserializing.
        private static readonly JsonSerializer _deserializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new PrivateSetterContractResolver(),
            Converters = { new Gene.GeneConverter() }
        });

        public override void WriteJson(JsonWriter writer, Individual value, JsonSerializer serializer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (value == null) throw new ArgumentNullException(nameof(value));

            // Write properties directly using the existing writer/serializer — no nested serializer.
            writer.WriteStartObject();

            writer.WritePropertyName("GenePool");
            serializer.Serialize(writer, value.GenePool);

            writer.WritePropertyName("Fitness");
            writer.WriteValue(value.Fitness);

            writer.WritePropertyName("Probability");
            writer.WriteValue(value.Probability);

            writer.WritePropertyName("Generation");
            writer.WriteValue(value.Generation);

            if (value.Objectives != null)
            {
                writer.WritePropertyName("Objectives");
                serializer.Serialize(writer, value.Objectives);
            }

            writer.WritePropertyName("ParetoRank");
            writer.WriteValue(value.ParetoRank);

            writer.WritePropertyName("CrowdingDistance");
            writer.WriteValue(value.CrowdingDistance);

            writer.WriteEndObject();
        }

        public override Individual ReadJson(JsonReader reader, Type objectType, Individual existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            // Load once into JObject and deserialize directly — no intermediate string round-trip.
            var obj = JObject.Load(reader);
            return obj.ToObject<Individual>(_deserializer)
                   ?? throw new JsonSerializationException("Failed to deserialize Individual");
        }
    }
}