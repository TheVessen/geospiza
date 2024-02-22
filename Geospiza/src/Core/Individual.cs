using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Individual
{
    public List<Gene> GenePool { get; private set; }
    public double Fitness { get; private set; }
    public double Probability { get; private set; }
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

    public void AddStableGene(Gene gene)
    {
        GenePool.Add(gene);
    }
    public void SetFitness(double fitness)
    {
        Fitness = fitness;
    }
    
    public void SetProbability(double normalizedFitness)
    {
        Probability = normalizedFitness;
    }
    
    public void SetGeneration(int generation)
    {
        Generation = generation;
    }
    
    public void Reinstate()
    {
        var stateManager = StateManager.Instance;
        foreach (var gene in GenePool)
        {
            var matchingGene = stateManager.Genotype[gene.GeneGuid];
            matchingGene?.SetTickValue(gene.TickValue);
        }
    }

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