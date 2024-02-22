using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;
using Geospiza.Strategies.Termination;
using Grasshopper.Kernel.Data;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithm : EvolutionBlueprint
{
    private StateManager StateManager { get; set; }
    private Observer Observer { get; set; }
    public EvolutionaryAlgorithm(EvolutionaryAlgorithmSettings settings, StateManager stateManager, Observer observer) : base(settings)
    {
        StateManager = stateManager;
        Observer = observer;
    }

    public override void RunAlgorithm()
    {
        //Initialize the population
        InitializePopulation(StateManager, Observer);

        //Run the algorithm
        for (var i = 0; i < MaxGenerations - 1; i++)
        {
            var populationCopy = new Population(Population);
            
            //Create a mating pool
            var newPopulation = new Population();
            
            //Elitism
            var elite = SelectTopIndividuals(EliteSize);
            newPopulation.AddIndividuals(elite);

            while (newPopulation.Count < PopulationSize)
            {
                //Select individuals for mating pool
               List<Individual> matingPool = SelectionStrategy.Select(populationCopy, PopulationSize);

                //Pair individuals in the mating pool
                var matingPairs = PairingStrategy.PairIndividuals(matingPool);
                
                foreach (var pair in matingPairs)
                {
                    var children = new List<Individual>();
                    
                    //Crossover
                    if (!(Random.NextDouble() < CrossoverStrategy.CrossoverRate))
                    {
                        children = CrossoverStrategy.Crossover(pair.Item1, pair.Item2);
                    }else
                    {
                        children.Add(pair.Item1);
                        children.Add(pair.Item2);
                    }
                  
                    //Mutate the children
                    foreach (var child in children)
                    {
                        if (Random.NextDouble() < MutationStrategy.MutationRate)
                        {
                            MutationStrategy.Mutate(child);
                        }
                    }

                    // Set the generation of the children
                    foreach (var ind in matingPool)
                    {
                        ind.SetGeneration(i + 1);
                    }

                    // Add children to the new population
                    newPopulation.AddIndividuals(children);
                }

                // Add the mating pool to the new population
                newPopulation.AddIndividuals(matingPool);
            }
            
            if (newPopulation.Count > PopulationSize)
            {
                // Sort newPopulation based on fitness in ascending order
                newPopulation.Inhabitants.Sort((inhabitant1, inhabitant2) =>
                    inhabitant1.Fitness.CompareTo(inhabitant2.Fitness));
                // Remove the individuals with the worst fitness
                int removeCount = newPopulation.Count - PopulationSize;
                newPopulation.Inhabitants.RemoveRange(PopulationSize, removeCount);
            }
            //Test the population
            newPopulation.TestPopulation(StateManager);

            //Get stats of the current population
            StateManager.GetDocument().ExpirePreview(false);
            Observer.Snapshot(newPopulation);
            Observer.SetPopulation(newPopulation);
            Observer.UpdateGenerationCounter();

            if (i > 5)
            {
                if (TerminationStrategy.Evaluate(Observer)) break;
            }

            Population = newPopulation;
        }

        //At end of algorithm, reinstate the best individual
        var best = Population.SelectTopIndividuals(1);
        best[0].Reinstate(StateManager);
    }
}