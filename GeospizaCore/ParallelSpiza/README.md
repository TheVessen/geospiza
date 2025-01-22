# ParallelSpiza (Experimental) 🧬💩

> **Note**: This module is currently in early experimental phase

## Concept

ParallelSpiza aims to scale evolutionary computation by distributing the workload across multiple Rhino Compute instances:

1. Multiple Geospiza solvers run in parallel via RhinoCompute/Hops
2. Each solver explores different parts of the solution space
3. A central coordinator ([EvolutionCoordinator](./EvolutionCoordinator.cs)) manages:
   - Collection of individuals from all instances
   - Global fitness evaluation
   - Distribution of promising solutions back to instances

## Current Status

- ✅ Basic architecture defined
- ✅ Coordinator interface designed
- ❌ RhinoCompute integration
- ❌ Individual synchronization
- ❌ Load balancing
- ❌ Fault toleranc
