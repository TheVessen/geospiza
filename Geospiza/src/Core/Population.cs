using System.Collections.Generic;
using System.Linq;

namespace Geospiza.Core;

public class Population
{
    public List<Individual> Inhabitants { get; private set; } = new List<Individual>();
    public int Count => Inhabitants.Count;

    public void AddIndividual(Individual individual)
    {
        Inhabitants.Add(individual);
    }

    public void AddIndividuals(List<Individual> individual)
    {
        Inhabitants.AddRange(individual);
    }
    
    public Population()
    {
    }
    
    public Population(Population population)
    {
        Inhabitants = new List<Individual>(population.Inhabitants);
    }


    public void TestPopulation()
    {
        StateManager stateManager = StateManager.Instance;
        var doc = stateManager.GetDocument();

        foreach (var individual in Inhabitants)
        {
            foreach (var gene in individual.GenePool)
            {
                var matchingGene = stateManager.TemplateGenes[gene.GeneGuid];
                if (matchingGene != null)
                {
                    matchingGene.SetTickValue(gene.TickValue);
                }
            }
            stateManager.GetDocument().NewSolution(false);
            stateManager.GetDocument().ExpirePreview(false);
            stateManager.FitnessComponent.ExpireSolution(false);
            individual.SetFitness(stateManager.FitnessComponent.FitnessValue);
        }
    }

    public double CalculateTotalFitness()
    {
        return Inhabitants.Sum(ind => ind.Fitness);
    }

    public double GetAverageFitness()
    {
        return CalculateTotalFitness() / Count;
    }

    public Population SelectTopIndividuals(int eliteSize)
    {
        var sortedPopulation = Inhabitants.OrderByDescending(ind => ind.Fitness).ToList();
        var newPopulation = new Population();
        for (var i = 0; i < eliteSize; i++)
        {
            newPopulation.AddIndividual(sortedPopulation[i]);
        }

        return newPopulation;
    }

    public void CalculateProbability()
    {
        double totalFitness = CalculateTotalFitness();
        foreach (var individual in Inhabitants)
        {
            individual.SetProbability(individual.Fitness / totalFitness);
        }
    }
}