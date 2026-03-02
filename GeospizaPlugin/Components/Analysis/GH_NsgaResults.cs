using System;
using System.Drawing;
using System.Linq;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Analysis;

public class GH_NsgaResults : GH_Component
{
    public GH_NsgaResults()
        : base("NSGA Results", "NsgaResults",
            "Extracts result data from a multi-objective NSGA-II or NSGA-III evolutionary algorithm run",
            "Geospiza", "Analysis")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap Icon => Resources.Observer;

    public override Guid ComponentGuid => new("8A4F1C3E-B72D-4956-A031-5E9D6C2B7F84");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The EvolutionObserver from an NSGA solver", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        // 0
        pManager.AddGenericParameter("Individuals", "I",
            "All individuals from the final generation",
            GH_ParamAccess.list);

        // 1
        pManager.AddIntegerParameter("Diversity", "D",
            "Number of unique individuals per generation",
            GH_ParamAccess.list);

        // 2
        pManager.AddGenericParameter("All Generations", "AG",
            "All individuals across every generation. Path index = generation number.",
            GH_ParamAccess.tree);

        // 3
        pManager.AddGenericParameter("Pareto Front", "PF",
            "Rank-0 (Pareto-optimal) individuals from the final generation",
            GH_ParamAccess.list);

        // 4
        pManager.AddIntegerParameter("Pareto Front Size", "PFS",
            "Number of rank-0 individuals per generation",
            GH_ParamAccess.list);

        // 5
        pManager.AddIntegerParameter("Front Count", "FC",
            "Total number of Pareto fronts per generation",
            GH_ParamAccess.list);

        // Per-objective stats trees: path = objective index, values = per-generation series.
        // {0} = objective 0, {1} = objective 1, etc.

        // 6
        pManager.AddNumberParameter("Objective Min", "OMin",
            "Minimum value per objective per generation. Path = objective index.",
            GH_ParamAccess.tree);

        // 7
        pManager.AddNumberParameter("Objective Max", "OMax",
            "Maximum value per objective per generation. Path = objective index.",
            GH_ParamAccess.tree);

        // 8
        pManager.AddNumberParameter("Objective Mean", "OMean",
            "Mean value per objective per generation. Path = objective index.",
            GH_ParamAccess.tree);

        // 9
        pManager.AddNumberParameter("Hypervolume", "HV",
            "Hypervolume indicator per generation. Higher and increasing values indicate a better and improving Pareto front.",
            GH_ParamAccess.list);

        // 10
        pManager.AddTextParameter("Objective Names", "ON",
            "Names of each objective, taken from the Multi-Objective Fitness component's input labels. " +
            "Use these to label charts or annotate analysis outputs.",
            GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper wrapper = null;
        if (!DA.GetData(0, ref wrapper)) return;

        if (wrapper.Value is not EvolutionObserver obs)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not an EvolutionObserver.");
            return;
        }

        if (obs.CurrentPopulation == null || obs.CurrentPopulation.Count == 0) return;

        var isMultiObjective = obs.CurrentPopulation.Inhabitants.Count > 0
                               && obs.CurrentPopulation.Inhabitants[0].Objectives is { Length: > 0 };

        if (!isMultiObjective)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "Observer does not contain multi-objective data. Use the Results component for single-objective runs.");
            return;
        }

        DA.SetDataList(0, obs.CurrentPopulation.Inhabitants);
        DA.SetDataList(1, obs.NumberOfUniqueIndividuals);

        var tree = new GH_Structure<GH_ObjectWrapper>();
        for (var g = 0; g < obs.AllGenerations.Count; g++)
        {
            var path = new GH_Path(g);
            foreach (var snap in obs.AllGenerations[g])
                tree.Append(new GH_ObjectWrapper(snap), path);
        }

        DA.SetDataTree(2, tree);

        var paretoFront = obs.CurrentPopulation.Inhabitants
            .Where(ind => ind.ParetoRank == 0)
            .ToList();
        DA.SetDataList(3, paretoFront);
        DA.SetDataList(4, obs.ParetoFrontSizes);
        DA.SetDataList(5, obs.FrontCount);

        var objMin = new GH_Structure<GH_Number>();
        var objMax = new GH_Structure<GH_Number>();
        var objMean = new GH_Structure<GH_Number>();

        if (obs.ObjectiveStats.Count > 0 && obs.ObjectiveStats[0].Length > 0)
        {
            var objCount = obs.ObjectiveStats[0].Length;
            for (var m = 0; m < objCount; m++)
            {
                var path = new GH_Path(m);
                foreach (var genStats in obs.ObjectiveStats)
                {
                    objMin.Append(new GH_Number(genStats[m][0]), path);
                    objMax.Append(new GH_Number(genStats[m][1]), path);
                    objMean.Append(new GH_Number(genStats[m][2]), path);
                }
            }
        }

        DA.SetDataTree(6, objMin);
        DA.SetDataTree(7, objMax);
        DA.SetDataTree(8, objMean);
        DA.SetDataList(9, obs.Hypervolume);

        if (obs.ObjectiveNames != null)
            DA.SetDataList(10, obs.ObjectiveNames);
    }
}