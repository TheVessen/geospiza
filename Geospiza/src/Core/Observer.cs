using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Strategies;
using Geospiza.Strategies.Termination;

namespace Geospiza.Core;

public class Observer
{
    private static Observer _instance;
    public int CurrentGeneration { get; private set; }
    public Population CurrentPopulation { get; private set; }
    public List<double> AverageGenerationFitnessMap { get; private set; }
    public List<Individual> BestIndividuals { get; private set; } = new List<Individual>();

    public static Observer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Observer();
            }
            return _instance;
        }
    }
    
    public void FitnessSnapshot(Population currentPopulation)
    {
        if (AverageGenerationFitnessMap == null)
        {
            AverageGenerationFitnessMap = new List<double>();
        }
        
        var fitness = currentPopulation.GetAverageFitness();
        AverageGenerationFitnessMap.Add(fitness);
    }
    
    public void SetPopulation(Population population)
    {
        CurrentPopulation = population;
        var bestIndividual = CurrentPopulation.SelectTopIndividuals(1)[0];
        
        BestIndividuals.Add(bestIndividual);
    }

    public void UpdateGenerationCounter()
    {
        CurrentGeneration++;
    }
    
    public void Reset()
    {
        AverageGenerationFitnessMap = new List<double>();
        CurrentPopulation = null;
        CurrentGeneration = 0;
        BestIndividuals = new List<Individual>();
    }
    
    public Population GetCurrentPopulation()
    {
        return CurrentPopulation;
    }

}