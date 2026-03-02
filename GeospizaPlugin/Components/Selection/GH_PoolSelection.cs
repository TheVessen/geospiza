using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Selection;

public class GH_PoolSelection : GH_Component
{
    public GH_PoolSelection()
        : base("Pool Selection", "PS",
            "Performs a pool selection. In Pool Selection, each individual in the population is assigned a selection probability " +
            "proportional to its fitness. Then, a number of individuals are selected randomly based on these probabilities.",
            "Geospiza", "Selection Strategies")
    {
    }

    protected override Bitmap Icon => Resources.PoolSelection;

    public override Guid ComponentGuid => new("3C6ACC4C-5C95-4189-BC61-19A4D870E554");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Selection Strategy", "SS", "The selection strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var selection = new PoolSelection();

        DA.SetData(0, selection);
    }
}