using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Comonents;
using Geospiza.Core;
using Geospiza.Strategies;
using Grasshopper.Kernel;

namespace Geospiza.Algorythm;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
    //Inhabitants
    protected Population _population { get; set; } = new Population();
    protected Observer _observer = new Observer();

    // Parameters
    protected int _populationSize = 100;
    protected int _maxGenerationCount = 20;
    protected double _mutationRate = 0.02;
    protected double _crossoverRate = 0.75;
    protected int _eliteSize = 2;

    // Strategies
    protected ISelectionStrategy _selectionStrategy => new IsotropicSelection();
    protected ICrossoverStrategy _crossoverStrategy => new TwoPointCrossover();
    protected MutationStrategy _mutationStrategy => new PercentageMutation(_mutationRate, 0.10);

    // Random
    protected Random _random = new Random();

    public abstract void RunAlgorithm();
    public abstract void InitializePopulation();
    protected static StateManager _stateManager = StateManager.Instance;

    protected EvolutionBlueprint()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteSize"></param>
    /// <returns></returns>
    protected List<Individual> SelectTopIndividuals(int eliteSize)
    {
        // Sort the population by fitness in descending order
        var sortedPopulation = _population.Inhabitants.OrderByDescending(individual => individual.Fitness).ToList();

        // Take the top 'eliteSize' individuals
        return sortedPopulation.Take(eliteSize).ToList();
    }
    
}