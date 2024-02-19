using System.Collections.Generic;
using System.Linq;
using Geospiza.Core;
using Geospiza.Strategies.Termination;
using Grasshopper.Kernel.Data;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithm : EvolutionBlueprint
{
    public EvolutionaryAlgorithm(EvolutionaryAlgorithmSettings settings) : base(settings)
    {
    }

    public override void RunAlgorithm()
    {
        //Initialize the population
        InitializePopulation();
        var populationCopy = new Population(Population);

        //Run the algorithm
        for (var i = 0; i < MaxGenerations - 1; i++)
        {
            //Create a mating pool
            var newPopulation = new Population();

            while (newPopulation.Count < PopulationSize)
            {
                var topIndividuals = SelectTopIndividuals(EliteSize);
                newPopulation.AddIndividuals(topIndividuals);
                List<Individual> matingPool = SelectionStrategy.Select(populationCopy);

                var matingPairs = PairingStrategy.PairIndividuals(matingPool);

                foreach (var pair in matingPairs)
                {
                    if (!(Random.NextDouble() < CrossoverStrategy.CrossoverRate)) continue;
                    var children = CrossoverStrategy.Crossover(pair.Item1, pair.Item2);

                    // Apply mutation to each child
                    foreach (var child in children.Where(child => Random.NextDouble() < MutationStrategy.MutationRate))
                    {
                        MutationStrategy.Mutate(child);
                    }

                    foreach (var ind in matingPool)
                    {
                        ind.SetGeneration(i+1);
                    }

                    // Add children to the new population
                    newPopulation.AddIndividuals(children);
                }

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
            newPopulation.TestPopulation();

            //Get stats of the current population
            StateManager.GetDocument().ExpirePreview(false);
            Observer.FitnessSnapshot(newPopulation);
            Observer.SetPopulation(newPopulation);
            Observer.UpdateGenerationCounter();

            if (i > 5)
            {
                if (TerminationStrategy.Evaluate()) break;
            }

            Population = newPopulation;
        }

        //At end of algorithm, reinstate the best individual
        var best = Population.SelectTopIndividuals(1);
        best[0].Reinstate();
    }
}