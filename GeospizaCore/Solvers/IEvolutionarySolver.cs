using GeospizaCore.Core;

namespace GeospizaCore.Solvers;

public interface IEvolutionarySolver
{
    void InitializePopulation(StateManager stateManager, EvolutionObserver evolutionObserver);
    void RunAlgorithm(CancellationToken cancellationToken);
}