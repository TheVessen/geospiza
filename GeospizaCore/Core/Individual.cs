using GeospizaCore.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaCore.Core;

public class Individual : IEquatable<Individual>
{
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
        _genePool = new List<Gene>(genePool ?? throw new ArgumentNullException(nameof(genePool)));
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

        _genePool = new List<Gene>(individual.GenePool);
        GenePool = _genePool.AsReadOnly();
        Fitness = individual.Fitness;
        Probability = 0;
        Generation = individual.Generation;
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

    private int Generation { get; set; }

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
            stateManager.Genotype.TryGetValue(gene.GeneGuid, out var matchingGene);
            matchingGene?.SetTickValue(gene.TickValue, stateManager);
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
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Gene.GeneConverter() }
        };

        return JsonConvert.SerializeObject(this, settings);
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

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new PrivateSetterContractResolver(),
            Converters = new List<JsonConverter> { new Gene.GeneConverter() }
        };

        return JsonConvert.DeserializeObject<Individual>(json, settings)
               ?? throw new JsonSerializationException("Failed to deserialize Individual from JSON");
    }

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
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