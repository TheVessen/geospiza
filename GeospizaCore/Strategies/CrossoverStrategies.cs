using GeospizaCore.Core;
using Newtonsoft.Json;

namespace GeospizaCore.Strategies;

public interface ICrossoverStrategy
{
    public double CrossoverRate { get; set; }
    public List<Individual> Crossover(Individual parent1, Individual parent2);
    string ToJson();
    ICrossoverStrategy FromJson(string json);
}

public abstract class CrossoverStrategy : ICrossoverStrategy
{
    protected readonly Random Random = new();
    public double CrossoverRate { get; set; }

    /// <summary>
    ///     The crossover rate as originally set at construction time.
    ///     Never modified by <c>AdaptStrategies</c>, so it is always safe to read as the true baseline.
    /// </summary>
    public double InitialCrossoverRate { get; protected set; }

    public abstract List<Individual> Crossover(Individual parent1, Individual parent2);

    public string ToJson()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        return JsonConvert.SerializeObject(this, settings);
    }

    public ICrossoverStrategy FromJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        return JsonConvert.DeserializeObject<ICrossoverStrategy>(json, settings);
    }
}

/// <summary>
///     Single-point crossover: selects a random position in the gene sequence and
///     swaps the tails of two parents to produce two offspring.
/// </summary>
/// <remarks>
///     Reference: D. E. Goldberg, "Genetic Algorithms in Search, Optimization, and
///     Machine Learning," Addison-Wesley, 1989. ISBN 0-201-15767-5.
///     Also: https://en.wikipedia.org/wiki/Crossover_(genetic_algorithm)
/// </remarks>
public class SinglePointCrossover : CrossoverStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SinglePointCrossover" /> class.
    /// </summary>
    /// <param name="crossoverRate">The crossover rate.</param>
    public SinglePointCrossover(double crossoverRate)
    {
        CrossoverRate = crossoverRate;
        InitialCrossoverRate = crossoverRate;
    }

    /// <summary>
    ///     Performs a single point crossover between two parents.
    /// </summary>
    /// <param name="parent1">The first parent.</param>
    /// <param name="parent2">The second parent.</param>
    /// <returns>A list of offspring resulting from the crossover.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the parents have genomes of different lengths.</exception>
    /// <remarks>
    ///     Source: https://en.wikipedia.org/wiki/Crossover_(genetic_algorithm)
    /// </remarks>
    public override List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        var genePoolP1 = parent1.GenePool;
        var genePoolP2 = parent2.GenePool;

        if (genePoolP1.Count != genePoolP2.Count)
            throw new ArgumentException("Parents must have genomes of the same length");

        var genomeLength = genePoolP1.Count;
        if (genomeLength <= 1)
            return new List<Individual> { new Individual(parent1.GenePool), new Individual(parent2.GenePool) };

        var crossoverPoint = Random.Next(1, genomeLength);

        var child1Genome = new List<Gene>();
        var child2Genome = new List<Gene>();

        for (var i = 0; i < genomeLength; i++)
        {
            child1Genome.Add(i < crossoverPoint ? genePoolP1[i] : genePoolP2[i]);
            child2Genome.Add(i < crossoverPoint ? genePoolP2[i] : genePoolP1[i]);
        }

        var child1 = new Individual(child1Genome);
        var child2 = new Individual(child2Genome);

        return new List<Individual> { child1, child2 };
    }
}

/// <summary>
///     Uniform crossover: every gene is inherited independently from either parent with equal
///     probability; the second child receives the complementary gene at each position.
///     Carries no positional bias, unlike single-/two-point crossover — the right default when
///     gene order is arbitrary, as it is for Grasshopper slider/gene-pool lists.
/// </summary>
/// <remarks>
///     Reference: G. Syswerda, "Uniform Crossover in Genetic Algorithms," Proceedings of the
///     Third International Conference on Genetic Algorithms, pp. 2–9, 1989.
/// </remarks>
public class UniformCrossover : CrossoverStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UniformCrossover" /> class.
    /// </summary>
    /// <param name="crossoverRate">The crossover rate.</param>
    public UniformCrossover(double crossoverRate)
    {
        CrossoverRate = crossoverRate;
        InitialCrossoverRate = crossoverRate;
    }

    /// <summary>
    ///     Performs a uniform crossover between two parents.
    /// </summary>
    /// <param name="parent1">The first parent.</param>
    /// <param name="parent2">The second parent.</param>
    /// <returns>A list of offspring resulting from the crossover.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the parents have genomes of different lengths.</exception>
    public override List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        if (parent1.GenePool.Count != parent2.GenePool.Count)
            throw new ArgumentException("Parents must have genomes of the same length");

        var genomeLength = parent1.GenePool.Count;
        var child1Genome = new List<Gene>(genomeLength);
        var child2Genome = new List<Gene>(genomeLength);

        for (var i = 0; i < genomeLength; i++)
            if (Random.Next(2) == 0)
            {
                child1Genome.Add(parent1.GenePool[i]);
                child2Genome.Add(parent2.GenePool[i]);
            }
            else
            {
                child1Genome.Add(parent2.GenePool[i]);
                child2Genome.Add(parent1.GenePool[i]);
            }

        return new List<Individual> { new Individual(child1Genome), new Individual(child2Genome) };
    }
}

/// <summary>
///     Two-point crossover: selects two random positions in the gene sequence and
///     swaps the middle segment of two parents to produce two offspring.
///     Preserves more gene linkage structure than single-point crossover.
/// </summary>
/// <remarks>
///     Reference: D. E. Goldberg, "Genetic Algorithms in Search, Optimization, and
///     Machine Learning," Addison-Wesley, 1989. ISBN 0-201-15767-5.
/// </remarks>
public class TwoPointCrossover : CrossoverStrategy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TwoPointCrossover" /> class.
    /// </summary>
    /// <param name="crossoverRate">The crossover rate.</param>
    public TwoPointCrossover(double crossoverRate)
    {
        CrossoverRate = crossoverRate;
        InitialCrossoverRate = crossoverRate;
    }

    /// <summary>
    ///     Performs a two point crossover between two parents.
    /// </summary>
    /// <param name="parent1">The first parent.</param>
    /// <param name="parent2">The second parent.</param>
    /// <returns>A list of offspring resulting from the crossover.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the parents have genomes of different lengths.</exception>
    public override List<Individual> Crossover(Individual parent1, Individual parent2)
    {
        if (parent1.GenePool.Count != parent2.GenePool.Count)
            throw new ArgumentException("Parents must have genomes of the same length");

        var genomeLength = parent1.GenePool.Count;
        if (genomeLength < 2)
            return new List<Individual> { new Individual(parent1.GenePool), new Individual(parent2.GenePool) };

        // Pick two distinct points, then ensure point1 < point2
        var crossoverPoint1 = Random.Next(0, genomeLength);
        var crossoverPoint2 = Random.Next(0, genomeLength - 1);
        if (crossoverPoint2 >= crossoverPoint1) crossoverPoint2++;
        if (crossoverPoint1 > crossoverPoint2) (crossoverPoint1, crossoverPoint2) = (crossoverPoint2, crossoverPoint1);

        var child1Genome = new List<Gene>();
        var child2Genome = new List<Gene>();

        for (var i = 0; i < genomeLength; i++)
            if (i >= crossoverPoint1 && i <= crossoverPoint2)
            {
                child1Genome.Add(parent2.GenePool[i]);
                child2Genome.Add(parent1.GenePool[i]);
            }
            else
            {
                child1Genome.Add(parent1.GenePool[i]);
                child2Genome.Add(parent2.GenePool[i]);
            }

        var child1 = new Individual(child1Genome);
        var child2 = new Individual(child2Genome);

        return new List<Individual> { child1, child2 };
    }
}