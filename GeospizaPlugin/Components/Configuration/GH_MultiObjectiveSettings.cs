using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaCore.Solvers;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Configuration;

/// <summary>
///     Settings component for multi-objective solvers (NSGA-II, NSGA-III).
///     Pairing strategy is fixed per algorithm: NSGA-II always uses <see cref="RankAwarePairingStrategy" />,
///     NSGA-III always uses <see cref="ReferencePointPairingStrategy" />.
/// </summary>
public class GH_MultiObjectiveSettings : GH_Component
{
    public GH_MultiObjectiveSettings()
        : base("Multi-Objective Settings", "MOSettings",
            "Configure parameters and strategies for NSGA-II and NSGA-III multi-objective solvers.",
            "Geospiza", "Configuration")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.Settings;

    public override Guid ComponentGuid => new("63C53775-8907-48DB-A83F-54EFAD3F6301");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Population Size", "PS", "The size of the population", GH_ParamAccess.item, 50);
        pManager.AddNumberParameter("Max Generations", "MG", "The maximum number of generations",
            GH_ParamAccess.item, 50);
        pManager.AddGenericParameter("Crossover Strategy", "CS",
            "The crossover strategy. Default: TwoPointCrossover with rate 0.7.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Mutation Strategy", "MS",
            "The mutation strategy. Default: RandomMutation with rate 0.05.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Termination Strategy", "TS",
            "One or more termination strategies. The solver stops when any one triggers. Default: PopulationDiversity below 2.",
            GH_ParamAccess.list);

        pManager[2].Optional = true;
        pManager[3].Optional = true;
        pManager[4].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S",
            "Multi-objective settings for NSGA-II or NSGA-III.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double populationSize = 0;
        double maxGenerations = 0;

        GH_ObjectWrapper crossoverStrategyContainer = null;
        GH_ObjectWrapper mutationStrategyContainer = null;
        var terminationStrategyContainers = new List<GH_ObjectWrapper>();

        if (!DA.GetData(0, ref populationSize)) return;
        if (!DA.GetData(1, ref maxGenerations)) return;
        DA.GetData(2, ref crossoverStrategyContainer);
        DA.GetData(3, ref mutationStrategyContainer);
        DA.GetDataList(4, terminationStrategyContainers);

        if (populationSize <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Population size must be greater than 0");
            return;
        }
        if (maxGenerations <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Max generations must be greater than 0");
            return;
        }

        var crossoverStrategy = crossoverStrategyContainer?.Value as ICrossoverStrategy ?? new TwoPointCrossover(0.7);
        var mutationStrategy = mutationStrategyContainer?.Value as IMutationStrategy ?? new RandomMutation(0.05);
        var terminationStrategy = BuildTerminationStrategy(terminationStrategyContainers);

        WarnScalarTerminators(terminationStrategyContainers);

        // Pairing is set automatically by each solver (RankAwarePairing for NSGA-II, ReferencePointPairing for NSGA-III).
        // A placeholder is required here because SolverSettings.Validate() checks for a non-null pairing strategy.
        var settings = new SolverSettings
        {
            PopulationSize = Convert.ToInt32(populationSize),
            MaxGenerations = Convert.ToInt32(maxGenerations),
            EliteSize = 0,
            PairingStrategy = new ReferencePointPairingStrategy(),
            CrossoverStrategy = crossoverStrategy,
            MutationStrategy = mutationStrategy,
            TerminationStrategy = terminationStrategy,
            IsMultiObjective = true
        };

        DA.SetData(0, settings);
    }

    private void WarnScalarTerminators(List<GH_ObjectWrapper> containers)
    {
        foreach (var c in containers)
        {
            if (c?.Value is BestFitnessStagnation)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "BestFitnessStagnation uses scalar fitness to decide when to stop, which is unreliable for multi-objective runs. Prefer PopulationDiversity.");
            else if (c?.Value is ProgressConvergence)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "ProgressConvergence uses scalar fitness to decide when to stop, which is unreliable for multi-objective runs. Prefer PopulationDiversity.");
        }
    }

    private static ITerminationStrategy BuildTerminationStrategy(List<GH_ObjectWrapper> containers)
    {
        var strategies = new List<ITerminationStrategy>();
        foreach (var c in containers)
            if (c?.Value is ITerminationStrategy s)
                strategies.Add(s);

        if (strategies.Count == 0) return new PopulationDiversity(2);
        if (strategies.Count == 1) return strategies[0];
        return new CompositeTermination(strategies);
    }
}
