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
    public bool IsRunning { get; set; }
    public int RunGenerations { get; private set; }
    public Population CurrentPopulation { get; private set; }
    public List<double> GenerationFitnessMap { get; private set; }

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

    public void UpdateGenerationCounter()
    {
        RunGenerations++;
    }
    
    public void Reset()
    {
        GenerationFitnessMap = new List<double>();
        CurrentPopulation = null;
        RunGenerations = 0;
    }
    
    public Population GetCurrentPopulation()
    {
        return CurrentPopulation;
    }

}