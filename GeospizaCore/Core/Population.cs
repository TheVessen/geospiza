using GeospizaCore.Utils;
using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GeospizaCore.Core;

/// <summary>
///     Represents a population of individuals in an evolutionary algorithm.
///     This class provides methods to manage and evaluate the population, including adding individuals,
///     testing the population, calculating fitness and diversity, and selecting top individuals.
/// </summary>
public class Population
{
    public Population()
    {
    }

    /// <summary>
    ///     Copy constructor
    /// </summary>
    /// <param name="population"></param>
    public Population(Population population)
    {
        Inhabitants = new List<Individual>(population.Inhabitants);
    }

    /// <summary>
    ///     List of individuals in the population
    /// </summary>
    public List<Individual> Inhabitants { get; } = new();

    /// <summary>
    ///     Number of individuals in the population
    /// </summary>
    public int Count => Inhabitants.Count;

    /// <summary>
    /// </summary>
    /// <param name="individual"></param>
    public void AddIndividual(Individual individual)
    {
        Inhabitants.Add(individual);
    }

    /// <summary>
    ///     Add a list of individuals to the population
    /// </summary>
    /// <param name="individual"></param>
    public void AddIndividuals(List<Individual> individual)
    {
        if (individual == null) throw new ArgumentNullException(nameof(individual));
        Inhabitants.AddRange(individual);
    }

    /// <summary>
    ///     Tests the population by evaluating the fitness of each individual and updating their fitness values.
    /// </summary>
    /// <param name="stateManager">The state manager containing the genotype and other state information.</param>
    /// <param name="evolutionObserver">The evolution observer used to track the best fitness values.</param>
    /// <exception cref="System.Exception">Thrown if the document or fitness component is null.</exception>
    public void TestPopulation(StateManager stateManager, EvolutionObserver evolutionObserver)
    {
        // Get the best fitness seen so far; Last() is O(1) and equals Max() when elitism preserves the best
        var bestFitness = evolutionObserver.BestFitness;
        var max = bestFitness.Count > 0 ? bestFitness[bestFitness.Count - 1] : double.MinValue;

        // Iterate through each individual in the population
        foreach (var individual in Inhabitants)
        {
            // Set the tick values for each gene in the individual's gene pool
            foreach (var gene in individual.GenePool)
            {
                var genotype = stateManager.Genotype;

                if (genotype == null) throw new Exception("Genotype is null for" + gene.GeneName);

                if (genotype.TryGetValue(gene.GeneGuid, out var matchingGene))
                    matchingGene.SetTickValue(gene.TickValue, stateManager);
            }

            // Get the document from the state manager
            var doc = stateManager.GetDocument();
            if (doc == null) throw new Exception("Document is null");

            // Solve the document based on the preview level
            if (stateManager.PreviewLevel == 0)
                doc.NewSolution(false);
            else
                doc.NewSolution(false, GH_SolutionMode.Silent);

            // Get the fitness component from the state manager
            var fitnessComponent = stateManager.FitnessComponent;
            if (fitnessComponent == null) throw new Exception("Fitness component is null");

            // Expire the solution of the fitness component to update the fitness value
            fitnessComponent.ExpireSolution(false);
            individual.SetFitness(Fitness.Instance.GetFitness());

            // If the preview level is 2, update the document preview if the individual's fitness is the new maximum
            if (stateManager.PreviewLevel != 2) continue;
            if (!(max < individual.Fitness)) continue;

            doc.ExpirePreview(true);
            max = individual.Fitness;
        }
    }

    /// <summary>
    ///     Tests the population for multi-objective optimization by evaluating each individual's objectives
    ///     and updating their <see cref="Individual.Objectives" /> values.
    ///     Also sets <see cref="Individual.Fitness" /> to <c>objectives[0]</c> for observer backward-compatibility.
    /// </summary>
    public void TestPopulationMultiObjective(StateManager stateManager, EvolutionObserver evolutionObserver)
    {
        var bestFitness = evolutionObserver.BestFitness;
        var max = bestFitness.Count > 0 ? bestFitness[bestFitness.Count - 1] : double.MinValue;

        foreach (var individual in Inhabitants)
        {
            foreach (var gene in individual.GenePool)
            {
                var genotype = stateManager.Genotype;
                if (genotype == null) throw new Exception("Genotype is null for " + gene.GeneName);
                if (genotype.TryGetValue(gene.GeneGuid, out var matchingGene))
                    matchingGene.SetTickValue(gene.TickValue, stateManager);
            }

            var doc = stateManager.GetDocument();
            if (doc == null) throw new Exception("Document is null");

            if (stateManager.PreviewLevel == 0)
                doc.NewSolution(false);
            else
                doc.NewSolution(false, GH_SolutionMode.Silent);

            var fitnessComponent = stateManager.FitnessComponent;
            if (fitnessComponent == null) throw new Exception("Fitness component is null");

            fitnessComponent.ExpireSolution(false);

            var objectives = Fitness.Instance.GetObjectives();
            individual.SetObjectives(objectives);
            if (objectives.Length > 0)
                individual.SetFitness(objectives[0]);

            if (stateManager.PreviewLevel != 2) continue;
            if (!(max < individual.Fitness)) continue;

            doc.ExpirePreview(true);
            max = individual.Fitness;
        }
    }

    /// <summary>
    ///     Gets the sum of the fitness values of all individuals in the population.
    /// </summary>
    /// <returns></returns>
    public double CalculateTotalFitness()
    {
        return Inhabitants.Sum(ind => ind.Fitness);
    }

    /// <summary>
    ///     Gets the average fitness of the population.
    /// </summary>
    /// <returns></returns>
    public double GetAverageFitness()
    {
        return CalculateTotalFitness() / Count;
    }

    /// <summary>
    ///     Calculates the diversity of the population by counting the number of unique individuals.
    /// </summary>
    /// <returns></returns>
    public int GetDiversity()
    {
        // HashSet<Individual> uses Equals() to resolve collisions, so distinct individuals
        // with the same hash code are still counted correctly.
        return new HashSet<Individual>(Inhabitants).Count;
    }

    /// <summary>
    ///     Selects the top individuals in the population based on their fitness values.
    /// </summary>
    /// <param name="eliteSize">Number of elite individuals to return</param>
    /// <returns></returns>
    public List<Individual> SelectTopIndividuals(int eliteSize)
    {
        var clampedSize = Math.Min(eliteSize, Inhabitants.Count);
        return Inhabitants.OrderByDescending(ind => ind.Fitness).Take(clampedSize).ToList();
    }

    /// <summary>
    ///     Calculates the probability of each individual in the population being selected for reproduction.
    ///     The probability is based on the individual's fitness relative to the total fitness of the population.
    /// </summary>
    public void CalculateProbability()
    {
        var totalFitness = CalculateTotalFitness();
        foreach (var individual in Inhabitants) individual.SetProbability(individual.Fitness / totalFitness);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 19;
            foreach (var individual in Inhabitants) hash = hash * 31 + individual.GetHashCode();

            return hash;
        }
    }

    /// <summary>
    ///     Tries to generate a population from a json string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static Population? FromJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new PrivateSetterContractResolver()
        };

        return JsonConvert.DeserializeObject<Population>(json, settings);
    }

    /// <summary>
    ///     Converts the population to a json string
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}