# Geospiza 🧬

A .NET library and Grasshopper plugin for evolutionary algorithms in architectural and engineering design.

## Overview

This project provides a flexible framework for implementing and running evolutionary algorithms in the Rhinoceros 3D/Grasshopper environment. It's designed to help solve and explore complex optimization problems in architectural and engineering design.

## Core Features

- Base evolutionary solvers with parallel computation support
- Customizable fitness functions
- Multiple selection, crossover, and mutation strategies
- Real-time evolution monitoring
- Integration with RhinoCompute
- Web preview support with Three.js (Windows only)

## Project Structure

The project is structured in 3 subprojects
<br>

- Geospiza Core
  - This is the core code it holds the main classes, and implements the strategies for the evultionary solver
- Geospiza Plugin
  - The main part for the grasshopper. It here are all the grasshopper component live
- Geospiza Server
  - Still a quite earlie idea. The goal of this project is to manage multiple grasshopper instances with rhinocompute to solve test solutions in a more scalable fassion.

## Installation

dotnet msbuild -target:Package -p:Configuration=Release .\GeospizaPlugin\GeospizaPlugin.csproj
(TODO)

## Usage

(TODO)

## Research References

See [Articles/README.md](Articles/README.md) for research papers and resources used in developing the algorithms.

## License

This project is licensed under the Mozilla Public License Version 2.0T License - see the [LICENSE.txt](LICENSE.txt) file for details

## Contributing

We welcome contributions of any size! Here's how you can help:

### Types of Contributions

- Bug fixes and improvements
- Documentation updates
- New features and enhancements
- Example files and tutorials
- Icon Design
- [Research and knowledge sharing ](Articles/)

### Getting Started

1. Fork and clone the repository
2. Set up your development environment
3. Pick an issue or propose a change
4. Make your changes in a new branch
5. Submit a pull request

### Development Process

- Write clean, documented code
- Add unit tests for new features
- Keep commits focused and clear
- Update documentation as needed
- Follow existing code style

### Pull Request Guidelines

- Use descriptive titles
- Link related issues
- Explain your changes
- Include test results
- Add screenshots for UI changes

### Community Standards

- Be respectful and inclusive
- Help review others' contributions
- Ask questions when unclear
- Join discussions
- Share knowledge

### Need Help?

- Check existing issues and discussions
- Review documentation
- Ask in discussions
- Contact maintainers

We value every contribution and aim to make the process smooth and enjoyable for everyone.
