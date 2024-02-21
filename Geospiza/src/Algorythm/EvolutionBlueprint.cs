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
using Grasshopper.Kernel.Types;

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
    protected PairingStrategy PairingStrategy { get; set; }
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
        var copiedPopulation = Population.Inhabitants.Select(individual => new Individual(individual)).ToList();

        // Take the top 'eliteSize' individuals
        return copiedPopulation.Take(eliteSize).ToList();
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
            var individual = new Individual();

            //Go through the gene pool and create a new individual
            foreach (var templateGene in StateManager.Genotype)
            {
                var ctg = templateGene.Value;
                ctg.SetTickValue(Random.Next(ctg.TickCount));

                var stableGene = new Gene(ctg.TickValue, ctg.GeneGuid,
                    ctg.TickCount, ctg.Name, ctg.GhInstanceGuid,
                    ctg.GenePoolIndex);

                individual.AddStableGene(stableGene);
            }

            StateManager.GetDocument().NewSolution(false);
            StateManager.GetDocument().ExpirePreview(false);
            StateManager.FitnessComponent.ExpireSolution(false);

            //Get fitness from the state state manager and apply it to the individual
            var currentFitness = StateManager.FitnessComponent.FitnessValue;
            individual.SetFitness(currentFitness);
            individual.SetGeneration(0);

            //Add the individual to the population
            newPopulation.AddIndividual(individual);
        }

        Observer.Snapshot(newPopulation);
        Observer.SetPopulation(newPopulation);
        Population = newPopulation;
    }
}