using Geospiza.Strategies;
using Geospiza.Strategies.Crossover;
using Geospiza.Strategies.Mutation;
using Geospiza.Strategies.Pairing;
using Geospiza.Strategies.Selection;
using Geospiza.Strategies.Termination;

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
    {
        // Set default values
        PopulationSize = 50;
        MaxGenerations = 100;
        EliteSize = 0;
        SelectionStrategy = new TournamentSelection(5,2); // Default selection strategy
        CrossoverStrategy = new TwoPointCrossover(0.7); // Default crossover strategy
        MutationStrategy = new PercentageMutation(0.01, 0.01); // Default mutation strategy
        PairingStrategy = new PairingStrategy(0); // Default pairing strategy
        TerminationStrategy = new ProgressConvergence(); // Default termination strategy
    }
}