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
        pManager.AddGenericParameter("Final Population", "FP",
            "All individuals from the final generation",
            GH_ParamAccess.list);

        // 1
        pManager.AddIntegerParameter("Diversity", "D",
            "Number of unique individuals per generation",
            GH_ParamAccess.list);

        // 2
        pManager.AddGenericParameter("Pareto Front", "PF",
            "Rank-0 (Pareto-optimal) individuals from the final generation",
            GH_ParamAccess.list);

        // 3
        pManager.AddIntegerParameter("Pareto Front Size", "PFS",
            "Number of rank-0 individuals per generation",
            GH_ParamAccess.list);

        // 4
        pManager.AddNumberParameter("Hypervolume", "HV",
            "Hypervolume indicator per generation. Higher and increasing values indicate a better and improving Pareto front.",
            GH_ParamAccess.list);

        // 5
        pManager.AddTextParameter("Objective Names", "ON",
            "Names of each objective, taken from the Multi-Objective Fitness component's input labels. " +
            "Use these to label charts or annotate analysis outputs.",
            GH_ParamAccess.list);

        // 6
        pManager.AddNumberParameter("Objective Values", "OV",
            "Tree of objective values per individual. Each branch {i} corresponds to individual i in the Final Population output, with one value per objective in the same order as Objective Names.",
            GH_ParamAccess.tree);
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

        var paretoFront = obs.CurrentPopulation.Inhabitants
            .Where(ind => ind.ParetoRank == 0)
            .ToList();
        DA.SetDataList(2, paretoFront);
        DA.SetDataList(3, obs.ParetoFrontSizes);
        DA.SetDataList(4, obs.Hypervolume);

        if (obs.ObjectiveNames != null)
            DA.SetDataList(5, obs.ObjectiveNames);

        var objectiveTree = new GH_Structure<GH_Number>();
        var inhabitants = obs.CurrentPopulation.Inhabitants;
        for (var i = 0; i < inhabitants.Count; i++)
        {
            var path = new GH_Path(i);
            var objectives = inhabitants[i].Objectives;
            if (objectives != null)
                foreach (var v in objectives)
                    objectiveTree.Append(new GH_Number(v), path);
        }
        DA.SetDataTree(6, objectiveTree);
    }
}
