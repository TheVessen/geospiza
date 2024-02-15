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

    public void TestPopulation()
    {
        StateManager stateManager = StateManager.Instance;
        var doc = stateManager.GetDocument();

        foreach (var individual in Inhabitants)
        {
            foreach (var gene in individual._genePool)
            {
                var matchingGene = stateManager.TemplateGenes[gene.GeneGuid];
                if (matchingGene != null)
                {
                    matchingGene.SetTickValue(gene.TickValue);
                }
            }

            individual.SetFitness(stateManager.FitnessComponent.FitnessValue);
            doc.NewSolution(false);
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