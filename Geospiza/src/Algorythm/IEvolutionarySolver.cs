using Geospiza.Core;

namespace Geospiza.Algorythm;

public interface IEvolutionarySolver
{
    void InitializePopulation(StateManager stateManager, EvolutionObserver evolutionObserver);
    void RunAlgorithm();
}