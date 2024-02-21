using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Geospiza.Comonents;
using Geospiza.Strategies;
using Geospiza.Strategies.Termination;

namespace Geospiza.Core;

public class Observer
{
    private static Observer _instance;
    public int CurrentGeneration { get; private set; }
    public Population CurrentPopulation { get; private set; }
    public List<double> AverageFitness { get; private set; } = new List<double>();
    public List<double> BestFitness { get; private set; } = new List<double>();
    public List<double> WorstFitness { get; private set; }  = new List<double>();
    public List<double> TotalFitness { get; private set; } = new List<double>();
    public List<int> NumberOfUniqueIndividuals { get; private set; } = new List<int>();
    public List<int> Diversity { get; private set; }    
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
    
    public void Snapshot(Population currentPopulation)
    {
        if (BestFitness == null)
        {
            BestFitness = new List<double>();
        }
        if (WorstFitness == null)
        {
            WorstFitness = new List<double>();
        }
        if (AverageFitness == null)
        {
            AverageFitness = new List<double>();
        } 
        if (TotalFitness == null)
        {
            TotalFitness = new List<double>();
        } 
        if (NumberOfUniqueIndividuals == null)
        {
            NumberOfUniqueIndividuals = new List<int>();
        }
        
        BestFitness.Add(currentPopulation.Inhabitants.Max(inh => inh.Fitness));
        WorstFitness.Add(currentPopulation.Inhabitants.Min(inh => inh.Fitness));
        AverageFitness.Add(currentPopulation.GetAverageFitness());
        TotalFitness.Add(currentPopulation.CalculateTotalFitness());
        NumberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());
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
        AverageFitness = new List<double>();
        CurrentPopulation = null;
        CurrentGeneration = 0;
        BestIndividuals = new List<Individual>();
        BestFitness = new List<double>();
        WorstFitness = new List<double>();
        TotalFitness = new List<double>();
        NumberOfUniqueIndividuals = new List<int>();
        Diversity = new List<int>();
    }
    
    public Population GetCurrentPopulation()
    {
        return CurrentPopulation;
    }

}