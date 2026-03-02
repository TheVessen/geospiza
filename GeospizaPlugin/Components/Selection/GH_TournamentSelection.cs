using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Selection;

public class GH_TournamentSelection : GH_Component
{
    public GH_TournamentSelection()
        : base("Tournament Selection", "TS",
            "Performs a tournament selection. In Tournament Selection, a subset of individuals is chosen " +
            "from the population, and the individual with the highest " +
            "fitness in this group is selected.The process is repeated until the desired number of individuals is selected.",
            "Geospiza", "Selection Strategies")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.TournamentSelection;

    public override Guid ComponentGuid => new("BFD3F3A2-8FBE-4FE0-A392-6E02E9402F43");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Tournament Size", "TS", "The size of the tournament", GH_ParamAccess.item, 4);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Selection Strategy", "SS", "The selection strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double tournamentSize = 0;
        if (!DA.GetData(0, ref tournamentSize)) return;

        var tournamentSizeInt = Convert.ToInt32(tournamentSize);
        var selection = new TournamentSelection(tournamentSizeInt);

        DA.SetData(0, selection);
    }
}