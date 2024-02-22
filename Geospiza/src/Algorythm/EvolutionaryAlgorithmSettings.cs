using Geospiza.Strategies;
using Geospiza.Strategies.Crossover;
using Geospiza.Strategies.Mutation;
using Geospiza.Strategies.Pairing;
using Geospiza.Strategies.Selection;
using Geospiza.Strategies.Termination;
using Newtonsoft.Json;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithmSettings
{
    public ISelectionStrategy SelectionStrategy { get; init; }
    public ICrossoverStrategy CrossoverStrategy { get; init; }
    public IMutationStrategy MutationStrategy { get; init; }
    public PairingStrategy PairingStrategy { get; init; }
    public ITerminationStrategy TerminationStrategy { get; init; }
    public int PopulationSize { get; init; }
    public int MaxGenerations { get; init; }
    public int EliteSize { get; init; }
    
    // Constructor to initialize default values
    public EvolutionaryAlgorithmSettings()
    { }
    
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
    
    public static EvolutionaryAlgorithmSettings FromJson(string json)
    {
        return JsonConvert.DeserializeObject<EvolutionaryAlgorithmSettings>(json);
    }
}