using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GeospizaManager.Core;

public class Individual
{
    public Individual()
    {
        GenePool = new List<Gene>();
    }

    public Individual(List<Gene> genePool)
    {
        GenePool = genePool;
    }

    public Individual(Individual individual)
    {
        GenePool = individual.GenePool;
        Fitness = individual.Fitness;
        Probability = 0;
    }

    public Individual(string json)
    {
        var parsed = FromJson(json);
        GenePool = parsed.GenePool;
        Fitness = parsed.Fitness;
        Probability = parsed.Probability;
        Generation = parsed.Generation;
    }

    /// <summary>
    ///   Gene pool of the individual
    /// </summary>
    public List<Gene> GenePool { get; private set; }

    /// <summary>
    ///   Fitness of the individual
    /// </summary>
    public double Fitness { get; private set; }

    /// <summary>
    ///   How likely the individual is to be selected for reproduction
    /// </summary>
    public double Probability { get; private set; }

    /// <summary>
    ///   Generation of the individual
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>
    ///   Add a gene to the gene pool
    /// </summary>
    /// <param name="gene"></param>
    public void AddStableGene(Gene gene)
    {
        GenePool.Add(gene);
    }

    /// <summary>
    ///   Set the fitness of the individual
    /// </summary>
    /// <param name="fitness"></param>
    public void SetFitness(double fitness)
    {
        Fitness = fitness;
    }

    /// <summary>
    ///   Set the probability of the individual
    /// </summary>
    /// <param name="normalizedFitness"></param>
    public void SetProbability(double normalizedFitness)
    {
        Probability = normalizedFitness;
    }

    /// <summary>
    ///   Set the generation of the individual
    /// </summary>
    /// <param name="generation"></param>
    public void SetGeneration(int generation)
    {
        Generation = generation;
    }

    /// <summary>
    ///   Reinstate the state of the individual based on the provided state manager.
    /// </summary>
    /// <param name="stateManager">The state manager containing the genotype to match with the individual's gene pool.</param>
    public void Reinstate(StateManager stateManager)
    {
        // Iterate over each gene in the individual's gene pool
        foreach (var gene in GenePool)
        {
            // Find the matching gene in the state manager's genotype
            var matchingGene = stateManager.Genotype[gene.GeneGuid];

            // If a matching gene is found, set its tick value to the one from the individual's gene
            matchingGene?.SetTickValue(gene.TickValue, stateManager);
        }
    }

    /// <summary>
    ///   Reinstate the individual in the gh canvas
    /// </summary>
    /// <param name="doc"></param>
    /// <exception cref="System.Exception"></exception>
    public void Reinstate(GH_Document doc)
    {
        foreach (var gene in GenePool)
        {
            var slider = doc.FindObject(gene.GhInstanceGuid, true);
            if (slider == null) throw new Exception("Gene not found in document.");

            var type = slider.GetType().ToString();
            if (type == "Grasshopper.Kernel.Special.GH_NumberSlider")
            {
                var numberSlider = (GH_NumberSlider)slider;
                numberSlider.TickValue = gene.TickValue;
            }
            else if (type == "GalapagosComponents.GalapagosGeneListObject")
            {
                var genePool = (dynamic)slider;
                genePool.set_TickValue(gene.GenePoolIndex, gene.TickValue);
            }
            else
            {
                throw new Exception("Gene type not recognized.");
            }
        }
    }

    public override int GetHashCode()
    {
        var hash = 17;

        foreach (var gene in GenePool) hash = hash * 31 + gene.GetHashCode();
        hash = hash * 31 + Fitness.GetHashCode();

        return hash;
    }

    /// <summary>
    ///   Returns a string representation of the individual
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                IgnoreSerializableInterface = true,
                IgnoreSerializableAttribute = true
            }
        };

        var obj = new
        {
            Fitness,
            GenePool,
            Generation
        };

        return JsonConvert.SerializeObject(obj, settings);
    }

    public static Individual FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Individual>(json);
    }
}