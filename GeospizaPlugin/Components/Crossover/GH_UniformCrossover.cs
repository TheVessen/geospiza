using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Crossover;

public class GH_UniformCrossover : GH_Component
{
    public GH_UniformCrossover()
        : base("Uniform Crossover", "UC",
            "Combines two parents by inheriting each gene independently from either parent with equal probability, " +
            "producing two complementary offspring. Has no positional bias, which makes it a good default when the " +
            "order of the connected sliders/gene pools carries no meaning. " +
            "When crossover is skipped (controlled by the rate), both parents pass through unchanged.",
            "Geospiza", "Crossover Strategies")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap Icon => Resources.TwoPointCrosover;

    public override Guid ComponentGuid => new("8E2A41D7-5C3B-4E9F-A1D8-6B0F4C7E9A23");

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
            "Uniform crossover strategy — connect to the Settings component.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double crossoverRate = 0;
        if (!DA.GetData(0, ref crossoverRate)) return;

        var crossover = new UniformCrossover(crossoverRate);
        DA.SetData(0, crossover);
    }
}
