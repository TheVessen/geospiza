using Geospiza.Strategies;

namespace Geospiza.Algorythm;

public class EvolutionaryAlgorithmSettings
{
    public ISelectionStrategy SelectionStrategy { get; set; }
    public ICrossoverStrategy CrossoverStrategy { get; set; }
    public MutationStrategy MutationStrategy { get; set; }
    public int PopulationSize { get; set; }
    public double CrossoverRate { get; set; }
    public double MutationRate { get; set; }
    public int MaxGenerations { get; set; }
    public int EliteSize { get; set; }
    
    // Constructor to initialize default values
    public EvolutionaryAlgorithmSettings()
    {
        PopulationSize = 100;
        CrossoverRate = 0.7;
        MutationRate = 0.01;
        MaxGenerations = 100;
        EliteSize = 2;
        // Set default values
        SelectionStrategy = new TournamentSelection(5); // Default selection strategy
        CrossoverStrategy = new SinglePointCrossover(); // Default crossover strategy
        MutationStrategy = MutationStrategies.BasicMutation; // Default mutation strategy
    }
}