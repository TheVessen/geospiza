using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Geospiza.Core;
using Geospiza.Strategies;
using Grasshopper.GUI.Base;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithm: EvolutionBlueprint
{
    public EvolutionaryAlgorithm(EvolutionaryAlgorithmSettings settings) : base(settings)
    {
    }

    public override void RunAlgorithm()
    {
        
        //Initialize the population
        InitializePopulation();
        
        //Run the algorithm
        for (var i = 0; i < MaxGenerations -1; i++)
        {
            //Create a mating pool
            var newPopulation = new Population();
        
            while (newPopulation.Count < PopulationSize)
            {
                var topIndividuals = SelectTopIndividuals(EliteSize);
                newPopulation.AddIndividuals(topIndividuals);
                List<Individual> matingPool = SelectionStrategy.Select(Population);
                
                var matingPairs = PairingStrategy.PairIndividuals(matingPool, 0);
                
                foreach (var pair in matingPairs)
                {
                    if (Random.NextDouble() < CrossoverRate)
                    {
                        var children = CrossoverStrategy.Crossover(pair.Item1, pair.Item2);

                        // Apply mutation to each child
                        foreach (var child in children)
                        {
                            if (Random.NextDouble() < MutationRate)
                            {
                                MutationStrategy.Mutate(child);
                            }
                        }

                        // Add children to the new population
                        newPopulation.AddIndividuals(children);
                    }
                }
                
                //Create mutation
                foreach (var individual in matingPool)
                {
                    if (Random.NextDouble() < MutationRate)
                    {
                        MutationStrategy.Mutate(individual);
                    }
                }
                newPopulation.AddIndividuals(matingPool);
            }
            
            //Test the population
            newPopulation.TestPopulation();
            
            //Get stats of the current population
            StateManager.GetDocument().ExpirePreview(false);
            Observer.FitnessSnapshot(newPopulation);
            Observer.SetPopulation(newPopulation);

            if (i > 10)
            {
                var currentProgress = Observer.AssessProgress();
                if(currentProgress < 0.1)
                {
                    break;
                }
            }
            
            StateManager._thisComponent.Params.Output[0].AddVolatileData(new GH_Path(0), 0, i);
            
            Population = newPopulation;
        }
        
        var best = Population.SelectTopIndividuals(1);
        best.Inhabitants[0].ReinstateGene();
        
    }

}