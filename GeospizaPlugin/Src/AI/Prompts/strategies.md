## Available Geospiza Strategies

### Algorithm

- SingleObjective: Standard GA, maximises a single scalar fitness value (higher = better).
- NSGA2: Multi-objective GA using Pareto rank + crowding distance; good for 2-3 objectives.
- NSGA3: Multi-objective GA using reference points; scales better for 3+ objectives.

### Selection (SingleObjective only — NSGA-II and NSGA-III use internal tournament selection, do NOT suggest changing this for multi-objective runs)

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

- For SingleObjective only: PairingStrategy (Inbreeding) — configurable via InBreedingFactor (negative = similar parents, positive = dissimilar parents).
- For NSGA-II and NSGA-III: pairing is hardcoded per algorithm. Do NOT suggest changing it.

### Termination

- ProgressConvergence: Stops when improvement rate drops below threshold.
- BestFitnessStagnation: Stops after N generations without improvement.
- PopulationDiversity: Stops when unique individuals drop too low.
