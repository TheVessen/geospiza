using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Individual
{
    /// <summary>
    /// Gene pool of the individual
    /// </summary>
    public List<Gene> GenePool { get; private set; }
    /// <summary>
    /// Fitness of the individual
    /// </summary>
    public double Fitness { get; private set; }
    /// <summary>
    /// How likely the individual is to be selected for reproduction
    /// </summary>
    public double Probability { get; private set; }
    /// <summary>
    /// Generation of the individual
    /// </summary>
    public int Generation { get; private set; }
    
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

    /// <summary>
    /// Add a gene to the gene pool
    /// </summary>
    /// <param name="gene"></param>
    public void AddStableGene(Gene gene)
    {
        GenePool.Add(gene);
    }
    
    /// <summary>
    /// Set the fitness of the individual
    /// </summary>
    /// <param name="fitness"></param>
    public void SetFitness(double fitness)
    {
        Fitness = fitness;
    }
    
    /// <summary>
    /// Set the probability of the individual
    /// </summary>
    /// <param name="normalizedFitness"></param>
    public void SetProbability(double normalizedFitness)
    {
        Probability = normalizedFitness;
    }
    
    /// <summary>
    /// Set the generation of the individual
    /// </summary>
    /// <param name="generation"></param>
    public void SetGeneration(int generation)
    {
        Generation = generation;
    }
    
    public void Reinstate(StateManager stateManager)
    {
        foreach (var gene in GenePool)
        {
            var matchingGene = stateManager.Genotype[gene.GeneGuid];
            matchingGene?.SetTickValue(gene.TickValue, stateManager);
        }
    }

    /// <summary>
    /// Reinstate the individual in the gh canvas
    /// </summary>
    /// <param name="doc"></param>
    /// <exception cref="Exception"></exception>
    public void Reinstate(GH_Document doc)
    {
        foreach (var gene in GenePool)
        {
            var slider = doc.FindObject(gene.GhInstanceGuid, true);
            if(slider == null)
            {
                throw new Exception("Gene not found in document.");
            }
            
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
            }else
            {
                throw new Exception("Gene type not recognized.");
            }
        }
    }
    
    public override int GetHashCode()
    {
        int hash = 17;

        foreach (var gene in GenePool)
        {
            hash = hash * 31 + gene.GetHashCode();
        }
        hash = hash * 31 + Fitness.GetHashCode();

        return hash;
    }
    
    /// <summary>
    /// Returns a string representation of the individual
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                IgnoreSerializableInterface = true,
                IgnoreSerializableAttribute = true
            }
        };

        var obj = new
        {
            Fitness = this.Fitness,
            GenePool = this.GenePool,
            Generation = this.Generation
        };

        return JsonConvert.SerializeObject(obj, settings);
    }
    
    public void FromJson(string json)
    {
        var individual =  JsonConvert.DeserializeObject<Individual>(json);
        Fitness = individual.Fitness;
        GenePool = individual.GenePool;
    }
}