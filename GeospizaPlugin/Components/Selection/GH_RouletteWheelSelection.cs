using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Selection;

public class GH_RouletteWheelSelection : GH_Component
{
    public GH_RouletteWheelSelection()
        : base("Roulette Wheel Selection", "RWS",
            "Performs a roulette wheel selection. In Roulette Wheel Selection, the fitness of an individual is used to assign a probability of selection." +
            "Think of it as a Roulette Wheel where each individual takes up a slice of the wheel, but the size of the slice is proportional to the individual's fitness.",
            "Geospiza", "Selection Strategies")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.RouletSelection;

    public override Guid ComponentGuid => new("BFD3F3A2-8FBE-4FE0-44EE-6E02E9402F43");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Selection Strategy", "SS", "The selection strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var selection = new RouletteWheelSelection();

        DA.SetData(0, selection);
    }
}