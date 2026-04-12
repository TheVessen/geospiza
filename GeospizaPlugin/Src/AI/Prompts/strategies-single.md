## Geospiza Strategies (Single-Objective)

### Solvers

- Standard Solver: Evaluates individuals sequentially. Good for fast fitness functions.
- Parallel Solver: Evaluates the population concurrently using multiple threads. Use this when the Grasshopper fitness evaluation is slow or uses heavy geometry.

### Selection

- TournamentSelection: Runs small tournaments; increase size for more selection pressure.
- RouletteWheelSelection: Fitness-proportionate; avoid when fitness values are very similar.
- PoolSelection: Pre-filters top-N candidates; useful for large noisy populations.
- StochasticUniversalSampling: Even-spread selection; low bias, good diversity preservation.

### Crossover

- SinglePointCrossover: Swaps gene tails at one cut point.
- TwoPointCrossover: Swaps segment between two cut points; preserves gene blocks at both ends.

### Mutation

- FixedValueMutation: Fixed step size; use when gene ranges are uniform.
- PercentageMutation: Step scales with gene range; better for mixed-range genes.
- RandomMutation: Full random replacement; aggressive exploration, good for escaping local optima.

### Pairing

- PairingStrategy (Inbreeding): Configurable via InBreedingFactor (negative = similar parents, positive = dissimilar parents).

### Termination

- ProgressConvergence: Stops when improvement rate drops below threshold.
- BestFitnessStagnation: Stops after N generations without improvement.
- PopulationDiversity: Stops when unique individuals drop too low.
