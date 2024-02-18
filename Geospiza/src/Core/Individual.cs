using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Individual
{
    public List<Gene> GenePool { get; private set; }
    public double Fitness { get; private set; }
    public double Probability { get; private set; }
    private Type Type { get;  set; }
    private Guid GhInstanceGuid { get;  set; }
    private int GenePoolIndex { get;  set; }
    
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
        Fitness = 0;
        Probability = 0;
        Type = individual.Type;
        GhInstanceGuid = individual.GhInstanceGuid;
        GenePoolIndex = individual.GenePoolIndex;
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
    
    public void Reinstate()
    {
        var stateManager = StateManager.Instance;
        foreach (var gene in GenePool)
        {
            var matchingGene = stateManager.TemplateGenes[gene.GeneGuid];
            matchingGene?.SetTickValue(gene.TickValue);
        }
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
            GenePool = this.GenePool
        };

        return JsonConvert.SerializeObject(obj, settings);
    }
}