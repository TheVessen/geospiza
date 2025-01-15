using GeospizaManager.Core;

namespace GeospizaManager.Solvers;

public interface IEvolutionarySolver
{
  void InitializePopulation(StateManager stateManager, EvolutionObserver evolutionObserver);
  void RunAlgorithm();
}