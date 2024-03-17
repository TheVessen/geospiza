using Geospiza.Strategies.Crossover;
using Geospiza.Strategies.Mutation;
using Geospiza.Strategies.Pairing;
using Geospiza.Strategies.Selection;
using Geospiza.Strategies.Termination;
using Newtonsoft.Json;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithmSettings
{
  // Constructor to initialize default values

  public ISelectionStrategy SelectionStrategy { get; set; }
  public ICrossoverStrategy CrossoverStrategy { get; set; }
  public IMutationStrategy MutationStrategy { get; set; }
  public PairingStrategy PairingStrategy { get; set; }
  public ITerminationStrategy TerminationStrategy { get; set; }
  public int PopulationSize { get; set; }
  public int MaxGenerations { get; set; }
  public int EliteSize { get; set; }

  public string ToJson()
  {
    return JsonConvert.SerializeObject(this, Formatting.Indented);
  }

  public static EvolutionaryAlgorithmSettings FromJson(string json)
  {
    return JsonConvert.DeserializeObject<EvolutionaryAlgorithmSettings>(json);
  }
}