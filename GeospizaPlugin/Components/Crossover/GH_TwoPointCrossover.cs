using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Crossover;

public class GH_TwoPointCrossover : GH_Component
{
    public GH_TwoPointCrossover()
        : base("Two Point Crossover", "TPC",
            "Combines two parents by cutting their gene sequence at two random positions and swapping the middle segment, " +
            "producing two offspring. Preserves more structure from each parent than single-point crossover. " +
            "When crossover is skipped (controlled by the rate), both parents pass through unchanged.",
            "Geospiza", "Crossover Strategies")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap Icon => Resources.TwoPointCrosover;

    public override Guid ComponentGuid => new("3560D235-F910-4BC0-9A10-28CBCF6C3B81");

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
            "Two-point crossover strategy — connect to the Settings component.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double crossoverRate = 0;
        if (!DA.GetData(0, ref crossoverRate)) return;

        var crossover = new TwoPointCrossover(crossoverRate);
        DA.SetData(0, crossover);
    }
}
