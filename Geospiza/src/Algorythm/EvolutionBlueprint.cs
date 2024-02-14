using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;
using Geospiza.Strategies;

namespace Geospiza.Algorythm;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
    
    //Inhabitants
    protected List<Individual> _population { get; set; }
    protected List<TemplateGene> _genePool { get; set; }

    // Parameters
    protected int _populationSize = 100;
    protected int _maxGenerationCount = 1000;
    protected double _mutationRate = 0.01;
    protected double _crossoverRate = 0.7;
    protected int _eliteSize = 0;

    // Strategies
    protected ISelectionStrategy _selectionStrategy;
    protected ICrossoverStrategy _crossoverStrategy;
    protected MutationStrategy _mutationStrategy;

    // Random
    protected Random _random = new Random();

    public abstract void RunAlgorithm();
    public abstract void InitializePopulation();
    
    protected EvolutionBlueprint(List<TemplateGene> genePool)
    {
        _genePool = genePool;
    }
    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteSize"></param>
    /// <returns></returns>
    protected List<Individual> SelectTopIndividuals(int eliteSize)
    {
        // Sort the population by fitness in descending order
        var sortedPopulation = _population.OrderByDescending(individual => individual.Fitness).ToList();

        // Take the top 'eliteSize' individuals
        return sortedPopulation.Take(eliteSize).ToList();
    }
}