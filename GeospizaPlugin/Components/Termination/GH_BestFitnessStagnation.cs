using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Termination;

public class GH_BestFitnessStagnation : GH_Component
{
    public GH_BestFitnessStagnation()
        : base("Best Fitness Stagnation", "BFS",
            "Terminates the evolutionary algorithm when the best fitness has not improved over the last N generations",
            "Geospiza", "Termination Strategies")
    {
    }

    protected override Bitmap Icon => Resources.TerminationGenetic;

    public override Guid ComponentGuid => new("F4468DD9-413E-402F-B132-E4A588FDD071");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Stagnation Generations", "SG",
            "Number of generations without improvement before terminating", GH_ParamAccess.item, 20);
        pManager.AddNumberParameter("Threshold", "T",
            "Minimum improvement required to count as progress", GH_ParamAccess.item, 1e-6);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Termination Strategy", "TS", "The termination strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int stagnationGenerations = 20;
        double threshold = 1e-6;
        if (!DA.GetData(0, ref stagnationGenerations)) return;
        if (!DA.GetData(1, ref threshold)) return;
        DA.SetData(0, new BestFitnessStagnation(stagnationGenerations, threshold));
    }
}
