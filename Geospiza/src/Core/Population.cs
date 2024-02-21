using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
    
    public void ReplaceIndividuals(List<Individual> individuals)
    {
        Inhabitants = individuals;
    }


    public void TestPopulation()
    {
        StateManager stateManager = StateManager.Instance;
        var doc = stateManager.GetDocument();

        foreach (var individual in Inhabitants)
        {
            foreach (var gene in individual.GenePool)
            {
                var matchingGene = stateManager.Genotype[gene.GeneGuid];
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
    
    public int GetDiversity()
    {
        int diversity = 0;
        HashSet<int> uniqueHashes = new HashSet<int>();

        foreach (var individual in Inhabitants)
        {
            if (uniqueHashes.Add(individual.GetHashCode()))
            {
                diversity++;
            }
        }

        return diversity;
    }

    public List<Individual> SelectTopIndividuals(int eliteSize)
    {
        var bestIndividuals = new List<Individual>();
        var sortedPopulation = Inhabitants.OrderByDescending(ind => ind.Fitness).ToList();
        for (var i = 0; i < eliteSize; i++)
        {
            bestIndividuals.Add(sortedPopulation[i]);
        }

        return bestIndividuals;
    }

    public void CalculateProbability()
    {
        double totalFitness = CalculateTotalFitness();
        foreach (var individual in Inhabitants)
        {
            individual.SetProbability(individual.Fitness / totalFitness);
        }
    }
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}