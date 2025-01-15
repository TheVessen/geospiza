# Geospiza 🧬

A .NET library and Grasshopper plugin for evolutionary algorithms in architectural and engineering design.

## Overview

Geospiza is an open-source evolutionary computation framework that integrates with Rhino/Grasshopper to help solve complex design optimization problems. It provides:

- Efficient evolutionary algorithm implementations
- Integration with Rhino Compute for geometry processing
- Parallel computation support
- Easy-to-use Grasshopper components
- Flexible fitness function definitions
- Support for multiple optimization strategies

## Getting Started

### Prerequisites

- Rhino 8.0 or later
- .NET 7.0 or later
- Visual Studio 2022 or later (for development)

### Installation

1. Download the latest release from the [releases page](releases)
2. Place the `GeospizaPlugin.gha` file in your Grasshopper Libraries folder
3. Place the `GeospizaCore.dll` in the same directory
4. Restart Rhino and Grasshopper

## Usage

Add Geospiza components from the "Geospiza" tab in Grasshopper. Basic workflow:

1. Define your design parameters using `GeneTemplate`
2. Set up your fitness function
3. Configure solver settings
4. Connect to an evolution strategy
5. Run the solver

See the [examples folder](examples) for sample definitions.

## Development

### Building from Source

1. Clone the repository:

```bash
git clone https://github.com/yourusername/Geospiza.git
cd Geospiza
```

2. Open the solution in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution
5. Copy the built assemblies to your Grasshopper Libraries folder

### Project Structure

- `src/GeospizaCore/` - Core evolution algorithm library
- `src/GeospizaGH/` - Grasshopper plugin components
- `src/GeospizaCompute/` - Rhino Compute integration
- `tests/` - Unit and integration tests
- `examples/` - Sample Grasshopper definitions

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards

- Follow C# coding conventions
- Include XML documentation for public APIs
- Add unit tests for new features
- Update documentation as needed

## Documentation

- [API Reference](docs/api/index.md)
- [User Guide](docs/guide/index.md)
- [Examples](docs/examples/index.md)

### Building Documentation

Documentation is built using DocFX. To build locally:

```bash
docfx docs/docfx.json
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Rhino.Compute](https://github.com/mcneel/compute.rhino3d) team
- Contributors and users of the project
