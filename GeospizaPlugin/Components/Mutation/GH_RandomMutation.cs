using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Mutation;

public class GH_RandomMutation : GH_Component
{
    public GH_RandomMutation()
        : base("RandomMutation", "RM",
            "Applies a random mutation to each gene in an individual's gene pool, where the new value of the gene is a " +
            "random number within the range of the gene's valid values. This strategy " +
            "introduces variability in the gene pool, aiding in the " +
            "exploration of the solution space.",
            "Geospiza", "Mutation Strategies")
    {
    }

    protected override Bitmap Icon => Resources.RandomMutation;

    public override Guid ComponentGuid => new("10050646-9E96-4EB4-82F8-10731E21DD23");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Mutation Rate", "MR", "The mutation rate", GH_ParamAccess.item, 0.03);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Mutation Strategy", "MS", "The mutation strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double mutationRate = 0;
        if (!DA.GetData(0, ref mutationRate)) return;

        var strategy = new RandomMutation(mutationRate);

        DA.SetData(0, strategy);
    }
}