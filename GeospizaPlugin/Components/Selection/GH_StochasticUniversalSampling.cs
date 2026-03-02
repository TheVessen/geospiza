using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Selection;

public class GH_StochasticUniversalSampling : GH_Component
{
    public GH_StochasticUniversalSampling()
        : base("Stochastic Universal Sampling", "SUS",
            "Performs a Stochastic Universal Sampling. In SUS, the fitness of each individual is used " +
            "to assign a probability of selection. However, instead of selecting individuals " +
            "one at a time, SUS selects all individuals at once by spreading out evenly spaced pointers over the population's",
            "Geospiza", "Selection Strategies")
    {
    }

    protected override Bitmap Icon => Resources.StochasticUniversalSampling;

    public override Guid ComponentGuid => new("03256F11-0CF3-4547-986A-F16EBC2CC6A5");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Selection Strategy", "SS", "The selection strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var selection = new StochasticUniversalSampling();
        DA.SetData(0, selection);
    }
}