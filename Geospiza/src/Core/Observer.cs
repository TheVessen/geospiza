using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Geospiza.Comonents;
using Geospiza.Strategies;
using Geospiza.Strategies.Termination;
using Newtonsoft.Json;

namespace Geospiza.Core;

public class Observer
{
    private static Dictionary<GH_BasicSolver, Observer> _instances = new Dictionary<GH_BasicSolver, Observer>();
    public int CurrentGeneration { get; private set; }
    public Population CurrentPopulation { get; private set; }
    public List<double> AverageFitness { get; private set; } = new List<double>();
    public List<double> BestFitness { get; private set; } = new List<double>();
    public List<double> WorstFitness { get; private set; }  = new List<double>();
    public List<double> TotalFitness { get; private set; } = new List<double>();
    public List<int> NumberOfUniqueIndividuals { get; private set; } = new List<int>();
    public List<int> Diversity { get; private set; }    
    public List<Individual> BestIndividuals { get; private set; } = new List<Individual>();
    public List<double> FitnessStandardDeviation { get; private set; } = new List<double>();

    public static Observer GetInstance(GH_BasicSolver solver)
    {
        if (!_instances.ContainsKey(solver))
        {
            _instances[solver] = new Observer();
        }
        return _instances[solver];
    }
    
    /// <summary>
    /// Creates a snapshot of the current population
    /// </summary>
    /// <param name="currentPopulation"></param>
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
        if (FitnessStandardDeviation == null)
        {
            FitnessStandardDeviation = new List<double>();
        }
        
        BestFitness.Add(currentPopulation.Inhabitants.Max(inh => inh.Fitness));
        WorstFitness.Add(currentPopulation.Inhabitants.Min(inh => inh.Fitness));
        AverageFitness.Add(currentPopulation.GetAverageFitness());
        TotalFitness.Add(currentPopulation.CalculateTotalFitness());
        NumberOfUniqueIndividuals.Add(currentPopulation.GetDiversity());
        
        // Calculate standard deviation of fitness
        double average = currentPopulation.GetAverageFitness();
        double sumOfSquaresOfDifferences = currentPopulation.Inhabitants.Select(val => (val.Fitness - average) * (val.Fitness - average)).Sum();
        double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / currentPopulation.Inhabitants.Count);

        FitnessStandardDeviation.Add(standardDeviation);
    }
    
    public void SetPopulation(Population population)
    {
        CurrentPopulation = population;
        var bestIndividual = CurrentPopulation.SelectTopIndividuals(1)[0];
        BestIndividuals.Add(bestIndividual);
    }

    /// <summary>
    /// Sets the new generation int
    /// </summary>
    public void UpdateGenerationCounter()
    {
        CurrentGeneration++;
    }
    
    /// <summary>
    /// Reset the observer
    /// </summary>
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
    
    /// <summary>
    /// Gets the current population
    /// </summary>
    /// <returns></returns>
    public Population GetCurrentPopulation()
    {
        return CurrentPopulation;
    }
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}