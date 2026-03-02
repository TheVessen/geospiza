using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Crossover;

public class GH_SinglePointCrossover : GH_Component
{
    public GH_SinglePointCrossover()
        : base("Single Point Crossover", "SPC",
            "Combines two parents by cutting their gene sequence at one random position and swapping the tails, " +
            "producing two offspring. When crossover is skipped (controlled by the rate), both parents pass through unchanged.",
            "Geospiza", "Crossover Strategies")
    {
    }

    protected override Bitmap Icon => Resources.SinglePointCrosover;

    public override Guid ComponentGuid => new("28BDC3CE-B8D8-4895-B165-4C442A197F05");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter(
            "Crossover Rate", "CR",
            "Probability (0–1) that two parents will exchange genes each generation. " +
            "At 1.0 every pair recombines; at 0.0 parents always pass through unchanged. " +
            "0.7 is a recommended starting point.",
            GH_ParamAccess.item, 0.7);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter(
            "Crossover Strategy", "CS",
            "Single-point crossover strategy — connect to the Settings component.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double crossoverRate = 0;
        if (!DA.GetData(0, ref crossoverRate)) return;

        var crossover = new SinglePointCrossover(crossoverRate);
        DA.SetData(0, crossover);
    }
}
