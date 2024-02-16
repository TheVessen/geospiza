using Geospiza.Strategies;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithmSettings
{
    public ISelectionStrategy SelectionStrategy { get; set; }
    public ICrossoverStrategy CrossoverStrategy { get; set; }
    public IMutationStrategy MutationStrategy { get; set; }
    public IPairingStrategy PairingStrategy { get; set; }
    public int PopulationSize { get; set; }
    public double CrossoverRate { get; set; }
    public double MutationRate { get; set; }
    public int MaxGenerations { get; set; }
    public int EliteSize { get; set; }
    
    // Constructor to initialize default values
    public EvolutionaryAlgorithmSettings()
    {
        // Set default values
        PopulationSize = 100;
        MaxGenerations = 100;
        CrossoverRate = 0.7;
        MutationRate = 0.01;
        EliteSize = 2;
        SelectionStrategy = new TournamentSelection(5,2); // Default selection strategy
        CrossoverStrategy = new TwoPointCrossover(); // Default crossover strategy
        MutationStrategy = new PercentageMutation(MutationRate, 0.01); // Default mutation strategy
        PairingStrategy = new InbreedingPairingStrategy(); // Default pairing strategy
    }
}