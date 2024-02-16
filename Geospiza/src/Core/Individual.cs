using System;
using System.Collections.Generic;

namespace Geospiza.Core;

public class Individual
{
    public List<Gene> GenePool { get; private set; }
    public double Fitness { get; private set; }
    public double Probability { get; private set; }
    
    public Individual()
    {
        GenePool = new List<Gene>();
    }
    
    public Individual(List<Gene> genePool)
    {
        GenePool = genePool;
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
    
    public void ReinstateGene()
    {
        var stateManager = StateManager.Instance;
        foreach (var gene in GenePool)
        {
            var matchingGene = stateManager.TemplateGenes[gene.GeneGuid];
            matchingGene?.SetTickValue(gene.TickValue);
        }
    }
}