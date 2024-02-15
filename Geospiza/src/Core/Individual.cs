using System;
using System.Collections.Generic;

namespace Geospiza.Core;

public class Individual
{
    public List<Gene> _genePool { get; private set; } = new List<Gene>();
    public double Fitness { get; private set; }
    public double Probability { get; private set; }
    
    public Individual()
    {
        _genePool = new List<Gene>();
    }
    
    public Individual(List<Gene> genePool)
    {
        _genePool = genePool;
    }
    
    public void AddStableGene(Gene gene)
    {
        _genePool.Add(gene);
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
        foreach (var gene in _genePool)
        {
            var matchingGene = stateManager.TemplateGenes[gene.GeneGuid];
            matchingGene?.SetTickValue(gene.TickValue);
        }
    }
}