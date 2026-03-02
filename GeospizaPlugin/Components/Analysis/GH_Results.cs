using System;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Analysis;

public class GH_Results : GH_Component
{
    public GH_Results()
        : base("Results", "Results",
            "Extracts result data from a single-objective evolutionary algorithm run",
            "Geospiza", "Analysis")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap Icon => Resources.Observer;

    public override Guid ComponentGuid => new("2D368D5B-DC50-432D-85DD-311435EF865C");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The EvolutionObserver from a solver", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I",
            "All individuals from the final generation",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Best Fitness", "BF",
            "Best fitness value per generation",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Average Fitness", "AF",
            "Average fitness value per generation",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Worst Fitness", "WF",
            "Worst fitness value per generation",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Std Deviation", "SD",
            "Fitness standard deviation per generation",
            GH_ParamAccess.list);
        pManager.AddIntegerParameter("Diversity", "D",
            "Number of unique individuals per generation",
            GH_ParamAccess.list);
        pManager.AddGenericParameter("All Generations", "AG",
            "All individuals across every generation. Path index = generation number.",
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

        DA.SetDataList(0, obs.CurrentPopulation.Inhabitants);
        DA.SetDataList(1, obs.BestFitness);
        DA.SetDataList(2, obs.AverageFitness);
        DA.SetDataList(3, obs.WorstFitness);
        DA.SetDataList(4, obs.FitnessStandardDeviation);
        DA.SetDataList(5, obs.NumberOfUniqueIndividuals);

        var tree = new GH_Structure<GH_ObjectWrapper>();
        for (var g = 0; g < obs.AllGenerations.Count; g++)
        {
            var path = new GH_Path(g);
            foreach (var snap in obs.AllGenerations[g])
                tree.Append(new GH_ObjectWrapper(snap), path);
        }

        DA.SetDataTree(6, tree);
    }
}