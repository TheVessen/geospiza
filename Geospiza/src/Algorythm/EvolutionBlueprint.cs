using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Comonents;
using Geospiza.Core;
using Geospiza.Strategies;
using Geospiza.Strategies.Crossover;
using Geospiza.Strategies.Mutation;
using Geospiza.Strategies.Pairing;
using Geospiza.Strategies.Selection;
using Geospiza.Strategies.Termination;
using Grasshopper.Kernel;

namespace Geospiza.Algorythm;

public abstract class EvolutionBlueprint : IEvolutionarySolver
{
    // Other shared properties
    protected readonly Random Random = new Random();
    protected static readonly StateManager StateManager = StateManager.Instance;
    protected Observer Observer { get; set; }
    
    //Inhabitants
    protected Population Population { get; set; } = new Population();

    // Parameters
    protected int PopulationSize { get; set; }
    protected int MaxGenerations { get; set; }
    protected int EliteSize { get; set; }

    // Strategies
    protected ISelectionStrategy SelectionStrategy { get; set; }
    protected ICrossoverStrategy CrossoverStrategy { get; set; }
    protected IMutationStrategy MutationStrategy { get; set; }
    protected IPairingStrategy PairingStrategy { get; set; }
    protected ITerminationStrategy TerminationStrategy { get; set; }
    
    /// <summary>
    /// Initializes the evolutionary algorithm with the given settings.
    /// </summary>
    /// <param name="settings"></param>
    protected EvolutionBlueprint(EvolutionaryAlgorithmSettings settings)
    {
        Observer = Observer.Instance;
        
        PopulationSize = settings.PopulationSize;
        MaxGenerations = settings.MaxGenerations;
        EliteSize = settings.EliteSize;
        SelectionStrategy = settings.SelectionStrategy;
        CrossoverStrategy = settings.CrossoverStrategy;
        MutationStrategy = settings.MutationStrategy;
        PairingStrategy = settings.PairingStrategy;
        TerminationStrategy = settings.TerminationStrategy;
        
    }
    
    /// <summary>
    /// Main method to run the evolutionary algorithm.
    /// </summary>
    public abstract void RunAlgorithm();

    /// <summary>
    /// Selects the top individuals from the population.
    /// </summary>
    /// <param name="eliteSize"></param>
    /// <returns></returns>
    protected List<Individual> SelectTopIndividuals(int eliteSize)
    {
        // Sort the population by fitness in descending order
        var sortedPopulation = Population.Inhabitants.OrderByDescending(individual => individual.Fitness).ToList();

        // Take the top 'eliteSize' individuals
        return sortedPopulation.Take(eliteSize).ToList();
    }
    
    /// <summary>
    /// Initializes the population for the evolutionary algorithm.
    /// </summary>
    public void InitializePopulation()
    {
        //Create an empty population
        var newPopulation = new Population();
        for (var i = 0; i < PopulationSize; i++)
        {
            //Create a new solution and individual
            StateManager.GetDocument().NewSolution(false);
            var individual = new Individual();
            
            //Go through the gene pool and create a new individual
            foreach (var templateGene in StateManager.TemplateGenes)
            {
                var currentTemplateGene = templateGene.Value;
                currentTemplateGene.SetTickValue(Random.Next(currentTemplateGene.TickCount));
                var stableGene = new Gene(currentTemplateGene.TickValue, currentTemplateGene.GeneGuid, currentTemplateGene.TickCount, currentTemplateGene.Name);
                individual.AddStableGene(stableGene);
            }
            
            //Get fitness from the state state manager and apply it to the individual
            double currentFitness = StateManager.FitnessComponent.FitnessValue;;
            individual.SetFitness(currentFitness);
            
            //Add the individual to the population
            newPopulation.AddIndividual(individual);
            
            //Scedule a new solution
            StateManager.GetDocument().ExpirePreview(false);
        }
        Observer.FitnessSnapshot(newPopulation);
        Population = newPopulation;
    }
}