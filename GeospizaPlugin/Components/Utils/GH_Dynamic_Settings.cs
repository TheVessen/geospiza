using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeospizaCore.Solvers;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Utils;

public class GH_Dynamic_Settings : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_Dynamic_Settings class.
  /// </summary>
  public GH_Dynamic_Settings()
    : base("Dynamic Settings", "DS",
      "Creates combinations of different settings and strategies for evolutionary algorithm optimization",
      "Geospiza", "Utils")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddNumberParameter("Population Sizes", "PS", "The size of the population", GH_ParamAccess.list);
    pManager.AddNumberParameter("Max Generations", "MG", "The maximum number of generations", GH_ParamAccess.list);
    pManager.AddNumberParameter("Elite Sizes", "ES", "The number of elite individuals. If 0 no elite will be picked",
      GH_ParamAccess.list);
    pManager.AddGenericParameter("Selection Strategies", "SS",
      "The selection strategy. As default StochasticUniversalSampling is used", GH_ParamAccess.list);
    pManager.AddGenericParameter("Pairing Strategies", "PS",
      "The pairing strategy. As default an InBreedingFactor of 0.2 and Manhattan distance will be used",
      GH_ParamAccess.list);
    pManager.AddGenericParameter("Crossover Strategies", "CS",
      "The crossover strategy. As default TwoPoint crossover will be used with a crossover rate of 0.6",
      GH_ParamAccess.list);
    pManager.AddGenericParameter("Mutation Strategies", "MS",
      "The mutation strategy. As default random mutation will be used with a mutation rate of 0.01",
      GH_ParamAccess.list);
    pManager.AddGenericParameter("Termination Strategies", "TS",
      "The termination strategy. As a default it will terminate if the population diversity falls below 2",
      GH_ParamAccess.list);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
      GH_ParamAccess.list);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var populationSizes = new List<GH_Number>();
    if (!DA.GetDataList(0, populationSizes)) return;

    var maxGenerations = new List<GH_Number>();
    if (!DA.GetDataList(1, maxGenerations)) return;

    var eliteSizes = new List<GH_Number>();
    if (!DA.GetDataList(2, eliteSizes)) return;

    var selectionStrategiesWrapper = new List<GH_ObjectWrapper>();
    if (!DA.GetDataList(3, selectionStrategiesWrapper)) return;
    var selectionStrategies = selectionStrategiesWrapper.Select(w => w.Value as SelectionStrategy).ToList();

    var pairingStrategiesWrapper = new List<GH_ObjectWrapper>();
    if (!DA.GetDataList(4, pairingStrategiesWrapper)) return;
    var pairingStrategies = pairingStrategiesWrapper.Select(w => w.Value as PairingStrategy).ToList();

    var crossoverStrategiesWrapper = new List<GH_ObjectWrapper>();
    if (!DA.GetDataList(5, crossoverStrategiesWrapper)) return;
    var crossoverStrategies = crossoverStrategiesWrapper.Select(w => w.Value as CrossoverStrategy).ToList();

    var mutationStrategiesWrapper = new List<GH_ObjectWrapper>();
    if (!DA.GetDataList(6, mutationStrategiesWrapper)) return;
    var mutationStrategies = mutationStrategiesWrapper.Select(w => w.Value as MutationStrategy).ToList();

    var terminationStrategiesWrapper = new List<GH_ObjectWrapper>();
    if (!DA.GetDataList(7, terminationStrategiesWrapper)) return;
    var terminationStrategies = terminationStrategiesWrapper.Select(w => w.Value as TerminationStrategy).ToList();

    var settingsList = (from populationSize in populationSizes
      from maxGeneration in maxGenerations
      from eliteSize in eliteSizes
      from selectionStrategy in selectionStrategies
      from pairingStrategy in pairingStrategies
      from crossoverStrategy in crossoverStrategies
      from mutationStrategy in mutationStrategies
      from terminationStrategy in terminationStrategies
      select new SolverSettings
      {
        PopulationSize = Convert.ToInt32(populationSize.Value),
        MaxGenerations = Convert.ToInt32(maxGeneration.Value),
        EliteSize = Convert.ToInt32(eliteSize.Value),
        SelectionStrategy = selectionStrategy,
        PairingStrategy = pairingStrategy,
        CrossoverStrategy = crossoverStrategy,
        MutationStrategy = mutationStrategy,
        TerminationStrategy = terminationStrategy
      }).ToList();

    DA.SetDataList(0, settingsList);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => null;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("B67627AC-919B-4176-823B-AA54D74104B0");
}