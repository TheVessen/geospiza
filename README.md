# Geospiza 🧬

A .NET library and Grasshopper plugin for evolutionary algorithms in architectural and engineering design.

## Overview

This project provides a flexible framework for implementing and running evolutionary algorithms in the Rhinoceros 3D/Grasshopper environment. It's designed to help solve complex optimization problems in architectural and engineering design.

## Core Features

- Base evolutionary solvers with parallel computation support
- Customizable fitness functions
- Multiple selection, crossover, and mutation strategies
- Real-time evolution monitoring
- Integration with RhinoCompute
- Web preview support with Three.js (Windows only)

## Project Structure

    Geospiza/
    ├── GeospizaCore/
    │   ├── Core/
    │   ├── Strategies/
    │   └── Solvers/
    ├── GeospizaPlugin/
    │   └── Components/
    └── GeospizaServer/

## Installation

dotnet msbuild -target:Package -p:Configuration=Release .\GeospizaPlugin\GeospizaPlugin.csproj
(TODO)

## Usage

(TODO)

## Research References

See [Articles/README.md](Articles/README.md) for research papers and resources used in developing the algorithms.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
