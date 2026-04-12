## Geospiza Strategies (Multi-Objective)

### Solvers

- NsgaIISolver: Multi-objective GA using Pareto rank + crowding distance; good for 2-3 objectives.
- NsgaIIISolver: Multi-objective GA using reference points; scales better for 3+ objectives. Requires `ReferencePointDivisions` to be set.

### Selection & Pairing

- For NSGA-II and NSGA-III, selection (tournament) and pairing are handled internally by the algorithm's crowding distance or reference points. Do NOT suggest changing the selection or pairing strategy.

### Crossover

- SinglePointCrossover: Swaps gene tails at one cut point.
- TwoPointCrossover: Swaps segment between two cut points; preserves gene blocks at both ends.

### Mutation

- FixedValueMutation: Fixed step size; use when gene ranges are uniform.
- PercentageMutation: Step scales with gene range; better for mixed-range genes.
- RandomMutation: Full random replacement; aggressive exploration, good for escaping local optima.

### Termination

- PopulationDiversity: Stops when unique individuals drop too low. (Highly recommended for multi-objective)
- ProgressConvergence / BestFitnessStagnation: WARNING - these use scalar fitness to decide when to stop, which is unreliable for multi-objective runs.
