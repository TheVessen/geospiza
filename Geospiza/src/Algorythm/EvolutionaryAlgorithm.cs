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

    public override void RunAlgorithm()
    {
        
        //Initialize the population
        InitializePopulation();
        
        //Run the algorithm
        for (var i = 0; i < _maxGenerationCount -1; i++)
        {
            //Create a mating pool
            var newPopulation = new Population();
        
            while (newPopulation.Count < _populationSize)
            {
                var topIndiv = SelectTopIndividuals(_eliteSize);
                newPopulation.AddIndividuals(topIndiv);
                List<Individual> matingPool = _selectionStrategy.Select(_population);
                
                var matingPairs = PairIndividuals(matingPool, 0);
                
                foreach (var pair in matingPairs)
                {
                    if (_random.NextDouble() < _crossoverRate)
                    {
                        var children = _crossoverStrategy.Crossover(pair.Item1, pair.Item2);

                        // Apply mutation to each child
                        foreach (var child in children)
                        {
                            if (_random.NextDouble() < _mutationRate)
                            {
                                _mutationStrategy.Mutate(child);
                            }
                        }

                        // Add children to the new population
                        newPopulation.AddIndividuals(children);
                    }
                }
                
                //Create mutation
                foreach (var indiviual in matingPool)
                {
                    if (_random.NextDouble() < _mutationRate)
                    {
                        _mutationStrategy.Mutate(indiviual);
                    }
                }
                newPopulation.AddIndividuals(matingPool);
            }
            
            //Test the population
            newPopulation.TestPopulation();
            
            //Get stats of the current population
            _stateManager.GetDocument().ExpirePreview(false);
            _observer.FitnessSnapshot(newPopulation);
            _observer.SetPopulation(newPopulation);

            if (i > 10)
            {
                var currentProgess = _observer.AssessProgress();
                if(currentProgess < 0.1)
                {
                    break;
                }
            }
            
            _stateManager._thisComponent.Params.Output[0].AddVolatileData(new GH_Path(0), 0, i);
            
            _population = newPopulation;
        }
        
        var best = _population.SelectTopIndividuals(1);
        best.Inhabitants[0].ReinstateGene();
        
    }

    public override void InitializePopulation()
    {
        var newPopulation = new Population();
        for (var i = 0; i < _populationSize; i++)
        {
            _stateManager.GetDocument().NewSolution(false);
            var individual = new Individual();
            
            //Go through the gene pool and create a new individual
            foreach (var templateGene in _stateManager.TemplateGenes)
            {
                var currentTemplateGene = templateGene.Value;
                currentTemplateGene.SetTickValue(_random.Next(currentTemplateGene.TickCount));
                var stableGene = new Gene(currentTemplateGene.TickValue, currentTemplateGene.GeneGuid, currentTemplateGene.TickCount);
                individual.AddStableGene(stableGene);
            }
            
            double currentFitness = _stateManager.FitnessComponent.FitnessValue;;
            
            individual.SetFitness(currentFitness);
            
            //Add the individual to the population
            newPopulation.AddIndividual(individual);
            
            
            //Scedule a new solution
            _stateManager.GetDocument().ExpirePreview(false);
        }
        _observer.FitnessSnapshot(newPopulation);
        _population = newPopulation;
    }
    
    public List<Tuple<Individual, Individual>> PairIndividuals(List<Individual> selectedIndividuals, double inBreedingFactor)
    {
        var pairs = new List<Tuple<Individual, Individual>>();
        var random = new Random();

        foreach (var individual in selectedIndividuals)
        {
            Individual mate = FindMate(individual, selectedIndividuals, inBreedingFactor);
            pairs.Add(new Tuple<Individual, Individual>(individual, mate));
        }

        return pairs;
    }

    public Individual FindMate(Individual individual, List<Individual> potentialMates, double inBreedingFactor)
    {
        // Sort potential mates by genomic distance
        var sortedMates = potentialMates.OrderBy(mate => CalculateGenomicDistance(individual, mate)).ToList();

        // Select mate based on in-breeding factor
        int mateIndex = (int)((inBreedingFactor + 1) / 2 * (sortedMates.Count - 1));
        return sortedMates[mateIndex];
    }

    public double CalculateGenomicDistance(Individual ind1, Individual ind2)
    {
        // Example: Euclidean distance calculation
        double distance = 0;
        for (int i = 0; i < ind1._genePool.Count; i++)
        {
            distance += Math.Pow(ind1._genePool[i].TickValue - ind2._genePool[i].TickValue, 2);
        }
        return Math.Sqrt(distance);
    }
}