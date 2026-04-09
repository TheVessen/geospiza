# Geospiza Solvers

Three solvers are available, each suited to a different type of optimization problem.
If you are new to Geospiza, start with the **Basic Solver**.

---

## Quick Start — Basic Solver

The Basic Solver is the simplest entry point. It optimizes a **single fitness value** — one number
that your Grasshopper script computes and passes to the `Fitness` component. The algorithm tries
to maximize that number over many generations.

**When to use it:**
- You have one clear goal (minimize deflection, maximize area, minimize material, etc.)
- You are new to evolutionary optimization
- You want fast, predictable results

**How it works (in plain terms):**

1. A random population of designs is generated (your sliders get randomized).
2. Each design is evaluated — Grasshopper computes and the Fitness component reads the result.
3. The best designs are kept (elitism), the rest are discarded.
4. New designs are created by combining (crossover) and slightly modifying (mutation) the survivors.
5. Steps 2–4 repeat for each generation until the max generation count is reached or a
   termination condition triggers.
6. At the end, the best design found is reinstated on your sliders.

**Typical settings to start with:**
- Population Size: 50
- Max Generations: 50
- Elite Size: 2
- Mutation: RandomMutation, rate 0.03
- Crossover: TwoPointCrossover, rate 0.7
- Termination: PopulationDiversity below 2

---

## NSGA-II — Two or Three Objectives

NSGA-II (Non-dominated Sorting Genetic Algorithm II) optimizes **multiple objectives at once**
instead of collapsing them into a single number. The result is not one best design, but a
**Pareto front** — a set of designs where improving one objective always comes at the cost of
another. You then pick the trade-off that suits your project.

**When to use it:**
- You have 2 or 3 competing objectives (e.g. structural performance vs. material use vs. geometry)
- You want to see the full range of trade-offs, not just one answer
- Population size 50–150

**How it differs from the Basic Solver:**

Instead of ranking designs by a single fitness score, NSGA-II ranks them by **Pareto dominance**:
a design is "better" only if it is at least as good on every objective and strictly better on at
least one. Designs that cannot be improved on any objective without sacrificing another end up on
the Pareto front (rank 0). Within each rank, **crowding distance** is used to prefer designs that
are more spread out, maintaining diversity across the front.

**Practical notes:**
- Requires a `Multi-Objective Fitness` component on the canvas (not the standard `Fitness` component)
- Use `RankAwarePairing` or `ReferencePointPairing` — standard `InbreedingPairing` ignores Pareto rank
- At the end, the design with the highest crowding distance on the rank-0 front is reinstated
  (most isolated = most representative of the frontier)

**Recommended settings:**
- Population Size: 50–100
- Divisions: n/a (NSGA-II does not use reference points)
- Mutation: RandomMutation, rate 0.03
- Crossover: TwoPointCrossover, rate 0.7

---

## NSGA-III — Four or More Objectives

NSGA-III extends NSGA-II for **many-objective problems** (4+ objectives). With more than 3
objectives, crowding distance becomes unreliable as a diversity measure — distances in
high-dimensional space lose their meaning. NSGA-III replaces crowding distance with
**structured reference points** spread uniformly across the objective space, explicitly
defining where solutions should be placed.

**When to use it:**
- You have 4 or more objectives
- You need guaranteed coverage of specific trade-off regions, including extremes
- You want solutions spread across the entire front rather than clustered at the densest region

**How it differs from NSGA-II:**

The selection step for the critical (last) front is replaced by **niche-preserving selection**:
each reference point tracks how many solutions are already assigned to it (its niche count).
When filling remaining slots, the algorithm preferentially picks solutions near the most
under-represented reference points, ensuring even coverage even in sparse regions of the front.

**The Divisions parameter:**

This controls how many reference points are generated. Too many reference points relative to
population size leaves most of them empty, causing unstable selection. Too few and diversity
collapses. Use these as a starting point:

| Objectives | Population | Divisions | ~Reference Points |
|-----------|------------|-----------|-------------------|
| 2         | any        | 4         | 5                 |
| 3         | 50–100     | 6         | 28                |
| 4         | 100–200    | 8         | 165               |
| 5+        | 200+       | 12        | 1001              |

**Practical notes:**
- Same component requirements as NSGA-II (needs `Multi-Objective Fitness`)
- For 2–3 objectives, NSGA-II will generally achieve higher hypervolume — use NSGA-III only
  when you specifically need coverage of extreme trade-off solutions or have 4+ objectives
- If the Pareto front jumps around between generations, reduce Divisions

---

## Comparison Summary

| | Basic Solver | NSGA-II | NSGA-III |
|---|---|---|---|
| Objectives | 1 | 2–3 | 4+ |
| Result | Single best design | Pareto front | Pareto front |
| Diversity mechanism | Elitism | Crowding distance | Reference points |
| Complexity | Low | Medium | High |
| Best for | Beginners, clear single goal | Most multi-objective problems | Many-objective, 4+ goals |
| Fitness component | `Fitness` | `Multi-Objective Fitness` | `Multi-Objective Fitness` |
