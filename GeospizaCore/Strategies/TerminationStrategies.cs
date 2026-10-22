using GeospizaCore.Core;
using GeospizaCore.Solvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeospizaCore.Strategies;

public interface ITerminationStrategy
{
    public double TerminationThreshold { get; set; }
    public bool Evaluate(EvolutionObserver evolutionObserver);
}

/// <summary>
///     Combines multiple termination strategies and fires when any one of them triggers.
/// </summary>
[JsonConverter(typeof(CompositeTerminationConverter))]
public class CompositeTermination : ITerminationStrategy
{
    private readonly List<ITerminationStrategy> _strategies;

    public CompositeTermination(IEnumerable<ITerminationStrategy>? strategies)
    {
        _strategies = strategies?.Where(s => s != null).ToList() ?? new List<ITerminationStrategy>();
    }

    public IReadOnlyList<ITerminationStrategy> Strategies => _strategies;

    // Not meaningful for composite — individual strategies have their own thresholds.
    public double TerminationThreshold { get; set; }

    public bool Evaluate(EvolutionObserver evolutionObserver)
        => _strategies.Any(s => s.Evaluate(evolutionObserver));

    /// <summary>
    ///     Writes each sub-strategy in the same Type/Value envelope <see cref="EvoSettingsConverter" /> uses,
    ///     since the interface-typed list cannot be deserialized without per-item type information.
    /// </summary>
    public sealed class CompositeTerminationConverter : JsonConverter<CompositeTermination>
    {
        public override void WriteJson(JsonWriter writer, CompositeTermination? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(TerminationThreshold));
            writer.WriteValue(value.TerminationThreshold);
            writer.WritePropertyName(nameof(Strategies));
            writer.WriteStartArray();
            foreach (var strategy in value._strategies)
                EvoSettingsConverter.WriteTyped(writer, strategy, serializer);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override CompositeTermination? ReadJson(JsonReader reader, Type objectType,
            CompositeTermination? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var obj = JObject.Load(reader);
            var strategies = (obj[nameof(Strategies)] as JArray ?? new JArray())
                .Select(t => EvoSettingsConverter.ReadTyped<ITerminationStrategy>(t, serializer)!);

            return new CompositeTermination(strategies)
            {
                TerminationThreshold = obj[nameof(TerminationThreshold)]?.Value<double>() ?? 0
            };
        }
    }
}

public abstract class TerminationStrategy : ITerminationStrategy
{
    public abstract bool Evaluate(EvolutionObserver evolutionObserver);
    public double TerminationThreshold { get; set; }
}

public class ProgressConvergence : TerminationStrategy
{
    public int ProgressRange { get; }

    public ProgressConvergence(double threshold = 0.1, int progressRange = 5)
    {
        TerminationThreshold = threshold;
        ProgressRange = progressRange;
    }

    public override bool Evaluate(EvolutionObserver evolutionObserver)
    {
        var averageFitness = evolutionObserver.AverageFitness;
        var bestFitness = evolutionObserver.BestFitness;

        // Need at least ProgressRange + 1 entries to compute ProgressRange deltas
        if (averageFitness.Count <= ProgressRange || bestFitness.Count <= ProgressRange) return false;

        var totalNormalizedDelta = 0.0;

        for (var i = 1; i <= ProgressRange; i++)
        {
            var averageDelta =
                Math.Abs(averageFitness[averageFitness.Count - i] - averageFitness[averageFitness.Count - (i + 1)]);
            var bestValue = Math.Abs(bestFitness[bestFitness.Count - i]);

            // If best fitness is zero, use the raw delta (cannot normalize)
            var normalizedDelta = bestValue > 0 ? averageDelta / bestValue : averageDelta;
            totalNormalizedDelta += normalizedDelta;
        }

        var finalNormalizedDelta = totalNormalizedDelta / ProgressRange;

        return finalNormalizedDelta < TerminationThreshold;
    }
}

/// <summary>
///     Termination strategy that fires when the best fitness has not improved
///     by more than <see cref="ITerminationStrategy.TerminationThreshold" /> over
///     the last <see cref="StagnationGenerations" /> generations.
/// </summary>
public class BestFitnessStagnation : TerminationStrategy
{
    public int StagnationGenerations { get; }

    public BestFitnessStagnation(int stagnationGenerations = 20, double threshold = 1e-6)
    {
        StagnationGenerations = stagnationGenerations;
        TerminationThreshold = threshold;
    }

    public override bool Evaluate(EvolutionObserver evolutionObserver)
    {
        var best = evolutionObserver.BestFitness;
        if (best.Count <= StagnationGenerations) return false;
        var recent = best[best.Count - 1];
        var baseline = best[best.Count - StagnationGenerations - 1];
        return Math.Abs(recent - baseline) <= TerminationThreshold;
    }
}

/// <summary>
///     Termination strategy based on the diversity of the population
/// </summary>
public class PopulationDiversity : TerminationStrategy
{
    public PopulationDiversity(double threshold = 1)
    {
        TerminationThreshold = threshold;
    }

    public override bool Evaluate(EvolutionObserver evolutionObserver)
    {
        var population = evolutionObserver.CurrentPopulation;
        var diversity = population.GetDiversity();

        return diversity <= TerminationThreshold;
    }
}