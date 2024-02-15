using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Strategies;

namespace Geospiza.Core;

public class Observer
{
    private static Observer _instance;
    private const int MaxGenerationsToTrack = 5;
    private Population _currentPopulation { get; set; }
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
        _currentPopulation = population;
    }
    
    public double AssessProgress()
    {
        return TerminationStrategy.AssessGeneticDiversity(_currentPopulation);
    }
    
    public void Reset()
    {
        GenerationFitnessMap = new List<double>();
    }    

}