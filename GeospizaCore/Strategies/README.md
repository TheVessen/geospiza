# Strategies

This directory contains the strategies used for the solvers in the GeospizaCore project.

## Overview

The strategies implemented here are based on various articles and research papers. For more detailed information, refer to the [Articles](../../Articles/README.md) directory.

## Selection Strategies

Selection strategies determine how individuals are chosen from the population for reproduction. The following selection strategies are implemented:

- **TournamentSelection**: Selects individuals based on a tournament among a subset of the population.
- **RouletteWheelSelection**: Selects individuals based on their fitness proportion.
- **PoolSelection**: Selects individuals based on their fitness probabilities.
- **StochasticUniversalSampling**: Ensures a spread of selection points, promoting diversity while still favoring fitter individuals.
- **ExclusiveSelection**: Selects the top percentage of individuals based on their fitness. Note that this method can lead to premature convergence.
- **IsotropicSelection**: Selects individuals randomly, maintaining maximum diversity but marked as obsolete due to performance issues.

## Termination Strategies

Termination strategies define the conditions under which the algorithm should stop. The following termination strategies are implemented:

- **ProgressConvergence**: Terminates the algorithm if the progress in fitness improvement falls below a certain threshold over a specified range of generations.
- **PopulationDiversity**: Terminates the algorithm if the diversity of the population falls below a certain threshold.

## Crossover Strategies

Crossover strategies define how genetic material is exchanged between parents to produce offspring. The following crossover strategies are implemented:

- **SinglePointCrossover**: Performs a single point crossover between two parents.
- **TwoPointCrossover**: Performs a two-point crossover between two parents.

## Mutation Strategies

Mutation strategies define how genetic material is altered in individuals. The following mutation strategies are implemented:

- **FixedValueMutation**: Applies a fixed value mutation to each gene in an individual's gene pool.
- **PercentageMutation**: Applies a percentage-based mutation to each gene in an individual's gene pool.
- **RandomMutation**: Applies a random mutation to each gene in an individual's gene pool.

## Pairing Strategies

Pairing strategies define how individuals are paired for reproduction. The following pairing strategy is implemented:

- **PairingStrategy**: Pairs individuals based on an in-breeding factor and a specified distance function (Euclidean or Manhattan).

## Elitism

Elitism strategies define how the top individuals are preserved across generations. The following elitism strategy is implemented:

- **SelectTopIndividuals**: Selects the top individuals from the population based on their fitness.
