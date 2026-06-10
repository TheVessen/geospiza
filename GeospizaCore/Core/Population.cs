using GeospizaCore.Utils;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino;

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
    ///     Deep copy constructor. Each <see cref="Individual"/> is cloned via its copy constructor
    ///     so callers can mutate the new population (genes, fitness, Pareto state) without
    ///     affecting the source. Selection-pool snapshots in solvers rely on this independence.
    /// </summary>
    /// <param name="population"></param>
    public Population(Population population)
    {
        if (population == null) throw new ArgumentNullException(nameof(population));
        Inhabitants = population.Inhabitants
            .Select(ind => new Individual(ind))
            .ToList();
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
    ///     Adds a single individual to the population.
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
    ///     Pass <paramref name="skipCount" /> to skip the first N individuals (e.g. elites already evaluated).
    /// </summary>
    /// <param name="stateManager">The state manager containing the genotype and other state information.</param>
    /// <param name="evolutionObserver">The evolution observer used to track the best fitness values.</param>
    /// <param name="skipCount">Number of leading individuals to skip (they retain their existing fitness).</param>
    /// <exception cref="System.Exception">Thrown if the document or fitness component is null.</exception>
    public void TestPopulation(StateManager stateManager, EvolutionObserver evolutionObserver, int skipCount = 0)
    {
        EvaluateIndividuals(stateManager, evolutionObserver, skipCount,
            individual => { individual.SetFitness(Fitness.Instance.GetFitness()); });
    }

    /// <summary>
    ///     Tests the population for multi-objective optimization by evaluating each individual's objectives
    ///     and updating their <see cref="Individual.Objectives" /> values.
    ///     Also sets <see cref="Individual.Fitness" /> to <c>objectives[0]</c> for observer backward-compatibility.
    ///     Pass <paramref name="skipCount" /> to skip the first N individuals (e.g. elites already evaluated).
    /// </summary>
    public void TestPopulationMultiObjective(StateManager stateManager, EvolutionObserver evolutionObserver, int skipCount = 0)
    {
        EvaluateIndividuals(stateManager, evolutionObserver, skipCount, individual =>
        {
            var objectives = Fitness.Instance.GetObjectives();
            individual.SetObjectives(objectives);
            if (objectives.Length > 0)
                individual.SetFitness(objectives[0]);
        });
    }

    /// <summary>
    ///     Shared evaluation loop: applies genes, solves the document, and delegates fitness assignment to the caller.
    ///     Skips the first <paramref name="skipCount" /> individuals without re-evaluating them.
    ///     When <see cref="StateManager.FitnessCache" /> is non-null, individuals whose gene-pool
    ///     tick sequence has already been evaluated reuse the cached result and skip the
    ///     Grasshopper solve entirely (the dominant per-individual cost).
    /// </summary>
    private void EvaluateIndividuals(StateManager stateManager, EvolutionObserver evolutionObserver,
        int skipCount, Action<Individual> assignFitness)
    {
        var bestFitness = evolutionObserver.BestFitness;
        var max = bestFitness.Count > 0 ? bestFitness[bestFitness.Count - 1] : double.MinValue;

        // Hoist out of the inner loop: these are constant for the whole evaluation pass.
        var doc = stateManager.GetDocument();
        if (doc == null) throw new Exception("Document is null");
        var fitnessComponent = stateManager.FitnessComponent;
        if (fitnessComponent == null) throw new Exception("Fitness component is null");
        var cache = stateManager.FitnessCache;

        for (var idx = skipCount; idx < Inhabitants.Count; idx++)
        {
            var individual = Inhabitants[idx];

            // Cache hit: apply the stored fitness/objectives and skip the entire GH solve.
            // Preview updates intentionally don't fire here — the document state need not match
            // the cached individual, so triggering ExpirePreview would render a stale geometry.
            if (cache != null && cache.TryGet(individual.GenePool, out var cached))
            {
                individual.SetFitness(cached.Fitness);
                if (cached.Objectives != null)
                    individual.SetObjectives(cached.Objectives);
                continue;
            }

            foreach (var gene in individual.GenePool)
            {
                if (gene.GenePoolIndex >= 0)
                {
                    if (stateManager.AllGenePools.TryGetValue(gene.GhInstanceGuid, out var genePool))
                    {
                        genePool.set_TickValue(gene.GenePoolIndex, gene.TickValue);
                        genePool.ExpireSolutionTopLevel(false);
                    }
                }
                else
                {
                    if (stateManager.AllSliders.TryGetValue(gene.GhInstanceGuid, out var slider))
                    {
                        slider.TickValue = gene.TickValue;
                        slider.ExpireSolutionTopLevel(false);
                    }
                }
            }

            if (stateManager.PreviewLevel == 0)
                doc.NewSolution(false);
            else
                doc.NewSolution(false, GH_SolutionMode.Silent);

            fitnessComponent.ExpireSolution(false);
            assignFitness(individual);

            // Store the fresh result for future hits. Done after assignFitness so we capture
            // whatever the action wrote (single-objective Fitness or multi-objective Objectives).
            cache?.Store(individual.GenePool, individual.Fitness, individual.Objectives);

            if (stateManager.PreviewLevel != 2) continue;
            if (!(max < individual.Fitness)) continue;

            doc.ExpirePreview(true);
            RhinoApp.Wait();
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
        return Count == 0 ? 0.0 : CalculateTotalFitness() / Count;
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
    ///     Calculates genotypic diversity as the mean pairwise gene distance, normalized to [0, 1].
    ///     Per gene the distance is |tickA - tickB| / tickCount, averaged over all genes and all
    ///     pairs of individuals. Unlike <see cref="GetDiversity" /> (unique count) this stays
    ///     informative when individuals are all slightly different: 0 means a fully converged
    ///     population, higher values mean broader spread across the search space.
    /// </summary>
    public double GetGenotypicDiversity()
    {
        var n = Inhabitants.Count;
        if (n < 2) return 0.0;
        var geneCount = Inhabitants[0].GenePool.Count;
        if (geneCount == 0) return 0.0;

        var sum = 0.0;
        var pairs = 0;
        for (var i = 0; i < n; i++)
        {
            var poolA = Inhabitants[i].GenePool;
            for (var j = i + 1; j < n; j++)
            {
                var poolB = Inhabitants[j].GenePool;
                var distance = 0.0;
                for (var g = 0; g < geneCount; g++)
                {
                    var range = poolA[g].TickCount;
                    if (range > 0)
                        distance += Math.Abs(poolA[g].TickValue - poolB[g].TickValue) / (double)range;
                }

                sum += distance / geneCount;
                pairs++;
            }
        }

        return sum / pairs;
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
    ///     Returns a hash code for this population based on its inhabitants.
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