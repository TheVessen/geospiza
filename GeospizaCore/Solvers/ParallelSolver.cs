﻿using GeospizaCore.Core;
using GeospizaCore.Strategies;

namespace GeospizaCore.Solvers;

// 💩 FOR THE MOMENT A MESS DO NOT USE! 

public class ParallelSolver : EvolutionBlueprint
{
    private const int TerminationEvaluationThreshold = 5;

    public ParallelSolver(SolverSettings settings, StateManager stateManager,
        EvolutionObserver evolutionObserver) :
        base(settings)
    {
        StateManager = stateManager;
        EvolutionObserver = evolutionObserver;
    }

    private StateManager StateManager { get; }
    private EvolutionObserver EvolutionObserver { get; }

    public override void RunAlgorithm(CancellationToken cancellationToken)
    {
        // Initialize the population
        InitializePopulation(StateManager, EvolutionObserver);

        try
        {
            // Run the algorithm for the specified number of generations
            for (var i = 0; i < MaxGenerations - 1; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Algorithm cancelled.");
                    break;
                }

                // Create a copy of the current population
                var populationCopy = new Population(Population);

                // Create a new population for the next generation
                var newPopulation = new Population();

                // Select the top individuals from the current population (elitism)
                var elite = Elitism.SelectTopIndividuals(EliteSize, Population.Inhabitants);
                newPopulation.AddIndividuals(elite);

                // Continue generating new individuals until the new population is full
                while (newPopulation.Count < PopulationSize)
                {
                    // Select individuals for the mating pool
                    var matingPool = SelectionStrategy.Select(populationCopy, PopulationSize);

                    // IndividualPair individuals in the mating pool
                    var matingPairs = PairingStrategy.PairIndividuals(matingPool);

                    // For each pair, perform crossover and mutation to generate new individuals
                    foreach (var pair in matingPairs)
                    {
                        // Perform crossover on the pair to generate children
                        var children = PerformCrossover(pair);

                        // Mutate the children
                        MutateChildren(children);

                        // Add the children to the new population
                        newPopulation.AddIndividuals(children);
                    }

                    // Add the individuals from the mating pool to the new population
                    newPopulation.AddIndividuals(matingPool);
                }

                // If the new population is larger than the specified size, remove the least fit individuals
                if (newPopulation.Count > PopulationSize)
                {
                    // Sort newPopulation based on fitness in ascending order
                    newPopulation.Inhabitants.Sort((inhabitant1, inhabitant2) =>
                        inhabitant1.Fitness.CompareTo(inhabitant2.Fitness));
                    // Remove the individuals with the worst fitness
                    var removeCount = newPopulation.Count - PopulationSize;
                    newPopulation.Inhabitants.RemoveRange(PopulationSize, removeCount);
                }

                // Set the generation number for each individual in the new population
                foreach (var inhabitant in newPopulation.Inhabitants) inhabitant.SetGeneration(i + 1);

                // Test the fitness of the new population
                newPopulation.TestPopulation(StateManager, EvolutionObserver);

                // Record statistics for the current population
                StateManager.GetDocument().ExpirePreview(false);
                EvolutionObserver.Snapshot(newPopulation);

                var populationHash = newPopulation.GetHashCode();

                //TODO: For multi processing here would be the point to send the observer to the main thread

                // If the termination condition is met, stop the algorithm
                if (i > TerminationEvaluationThreshold)
                    if (TerminationStrategy.Evaluate(EvolutionObserver))
                        break;

                // Replace the current population with the new population
                Population = newPopulation;
                if (StateManager.PreviewLevel == 1) StateManager.GetDocument().ExpirePreview(true);
            }

            // At the end of the algorithm, reinstate the best individual
            var best = Population.SelectTopIndividuals(1);
            best[0].Reinstate(StateManager);
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during the algorithm
            Console.WriteLine($@"An error occurred: {ex.Message}");
        }
    }

    private List<Individual> PerformCrossover(IndividualPair individualPair)
    {
        return PerformOperation(individualPair, CrossoverStrategy.CrossoverRate, CrossoverStrategy.Crossover);
    }

    private void MutateChildren(List<Individual> children)
    {
        PerformOperation(children, MutationStrategy.MutationRate, MutationStrategy.Mutate);
    }

    private List<Individual> PerformOperation(IndividualPair individualPair, double rate,
        Func<Individual, Individual, List<Individual>> operation)
    {
        if (!(Random.NextDouble() < rate))
            return operation(individualPair.Individual1, individualPair.Individual2);
        return new List<Individual> { individualPair.Individual1, individualPair.Individual2 };
    }

    private void PerformOperation(List<Individual> individuals, double rate, Action<Individual> operation)
    {
        foreach (var individual in individuals)
            if (Random.NextDouble() < rate)
                operation(individual);
    }
}