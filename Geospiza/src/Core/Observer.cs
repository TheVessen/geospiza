using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Strategies;
using Geospiza.Strategies.Termination;

namespace Geospiza.Core;

public class Observer
{
    private static Observer _instance;
    private const int MaxGenerationsToTrack = 5;
    private Population CurrentPopulation { get; set; }
    public List<double> GenerationFitnessMap { get; private set; }

    public static Observer Instance
    {
        get { return _instance ?? (_instance = new Observer()); }
    }
    
    public void FitnessSnapshot(Population currentPopulation)
    {
        if (GenerationFitnessMap == null)
        {
            GenerationFitnessMap = new List<double>();
        }
        
        var fitness = currentPopulation.GetAverageFitness();
        GenerationFitnessMap.Add(fitness);
    }
    
    public void SetPopulation(Population population)
    {
        CurrentPopulation = population;
    }
    
    
    public void Reset()
    {
        GenerationFitnessMap = new List<double>();
    }
    
    public Population GetCurrentPopulation()
    {
        return CurrentPopulation;
    }

}