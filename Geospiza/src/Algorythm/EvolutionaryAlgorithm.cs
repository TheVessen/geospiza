using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Geospiza.Core;
using Geospiza.Strategies;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithm: EvolutionBlueprint
{

    public EvolutionaryAlgorithm(List<TemplateGene> genePool) : base(genePool)
    {
    }
    
    public override void RunAlgorithm()
    {
        InitializePopulation();
        
        //Set population fitness
        
        
        for (var i = 0; i < _maxGenerationCount; i++)
        {
            var topIndividuals = SelectTopIndividuals(_eliteSize);
            var newPopulation = new List<Individual>(topIndividuals);

            while (newPopulation.Count < _populationSize)
            {
                var selectedIndividuals = _selectionStrategy.Select(_population, 4);
                var offspring = new List<Individual>();

                for (int j = 0; j < selectedIndividuals.Count; j += 2)
                {
                    if (_random.NextDouble() < _crossoverRate)
                    {
                        var children = _crossoverStrategy.Crosover(new List<Individual> { selectedIndividuals[j], selectedIndividuals[j + 1] });
                        offspring.AddRange(children);
                    }
                    else
                    {
                        offspring.Add(selectedIndividuals[j]);
                        offspring.Add(selectedIndividuals[j + 1]);
                    }
                }

                foreach (var individual in offspring)
                {
                    foreach (var gene in individual._genePool)
                    {
                        if (_random.NextDouble() < _mutationRate)
                        {
                            MutationStrategies.BasicMutation(gene);
                        }
                    }
                }

                newPopulation.AddRange(offspring);

                while (newPopulation.Count > _populationSize)
                {
                    newPopulation.RemoveAt(newPopulation.Count - 1);
                }
            }
            
            //TODO: Adjust the sliders
            //TODO: Set the fitness of the new population 

            _population = newPopulation;
        }
    }

    public override void InitializePopulation()
    {
        for (var i = 0; i < _populationSize; i++)
        {
            var individual = new Individual();
            foreach (var templateGene in _genePool)
            {
                var tickValue = _random.Next(templateGene.TickCount);
                var stableGene = new Gene(tickValue, templateGene.GeneGuid, templateGene.TickCount);
                individual.AddStableGene(stableGene);
            }
            _population.Add(individual);
        }
    }

}