# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Geospiza is an evolutionary algorithm framework for architectural/engineering design optimization, built for Rhinoceros 3D / Grasshopper. It consists of four subprojects:

- **GeospizaCore** — Core library: evolutionary algorithm classes, strategies, solver logic
- **GeospizaPlugin** — Grasshopper plugin (.gha) exposing Core functionality as GH components
- **GeospizaServer** — Experimental server for coordinating multiple Grasshopper/RhinoCompute instances
- **app** — SvelteKit web interface for the web-based solver with real-time 3D and chart visualization

## Commands

### Web App (`app/`)

```bash
cd app
npm run dev          # Start dev server
npm run build        # Production build
npm run preview      # Preview production build
npm run check        # Type-check with svelte-check
npm run check:watch  # Type-check in watch mode
```

No test suite exists yet for the web app.

### .NET Projects

```bash
# Build all C# projects (from repo root or project directory)
dotnet build GeospizaCore/GeospizaCore.csproj
dotnet build GeospizaPlugin/GeospizaPlugin.csproj
dotnet build GeospizaServer/GeospizaServer.csproj

# Run the server
dotnet run --project GeospizaServer/GeospizaServer.csproj
```

The plugin targets both `net7.0` and `net48` (Rhino 8 / legacy compatibility). Output is a `.gha` file for Grasshopper.

No automated test projects exist; testing is done manually through Grasshopper and browser.

## Architecture

### Evolutionary Algorithm (GeospizaCore)

The core algorithm follows the **Strategy pattern** — all algorithm phases are pluggable:

```
BaseSolver / EvolutionarySolver
├── SelectionStrategy    (Tournament, Roulette, Pool, Stochastic)
├── PairingStrategy      (Inbreeding-based)
├── CrossoverStrategy    (Single-point, Two-point)
├── MutationStrategy     (Fixed, Percentage, Random)
├── EliticStrategy       (top-N preservation)
└── TerminationStrategy  (Convergence, Diversity)
```

Key classes:
- `Individual` — holds `Gene[]` + `Fitness` + `Generation`
- `Population` — collection of individuals; calls `TestPopulation()` to evaluate fitness via Grasshopper
- `StateManager` — holds Grasshopper document state and `PreviewLevel` setting
- `EvolutionObserver` — snapshots each generation's stats for monitoring/serialization
- `BaseSolver.RunAlgorithm()` is the main loop; uses `CancellationToken` for cooperative cancellation

### Grasshopper Plugin (GeospizaPlugin)

Components are prefixed `GH_` and live in `Components/` subdirectories mirroring the strategy categories. Each wraps a Core class for use in the GH canvas. `GH_WebSolver` handles WebSocket communication; `GH_WebIndividual` serializes geometry for the web preview.

### Web App (app/src/)

- **Framework:** SvelteKit 2 + Svelte 5 (runes: `$state`, `$derived`, `$effect`)
- **Styling:** Tailwind CSS v4 (Vite plugin, no config file needed)
- **State:** Local Svelte 5 runes — no external store
- **Communication:** `lib/websocket.ts` — `WebSocketService extends EventEmitter`, auto-reconnects every 3s, sends `{ command: "run" | "cancel" }`
- **3D:** `lib/three-helpers.ts` — Three.js scene with OrbitControls, renders `MeshData[]` from solver
- **Charts:** `lib/d3-helpers.ts` — D3.js SVG fitness-over-generation chart
- **Types:** `lib/types.ts` — `SolverResponse` discriminated union (`running` | `done` | `canceled`), `MeshData`, `Material`

### WebSocket Protocol

The Grasshopper plugin acts as WebSocket server; the SvelteKit app is the client.

**Client → Server:**
```json
{ "command": "run" }
{ "command": "cancel" }
```

**Server → Client (streaming during run):**
```json
{ "status": "running", "meshes": [...], "fitness": 0.42, "currentGeneration": 5 }
{ "status": "done", "finalFitness": 0.12 }
{ "status": "canceled" }
```

### GeospizaServer (Experimental)

Early-stage coordinator for multi-instance RhinoCompute setups. `EvolutionCoordinator` manages multiple solver instances. Not production-ready.
