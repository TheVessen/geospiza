using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Pairing;

public class GH_RankAwarePairing : GH_Component
{
    public GH_RankAwarePairing()
        : base("Rank Aware Pairing", "RAP",
            "Pairs individuals within the same Pareto front (rank). " +
            "Keeps crossover between quality-equivalent solutions to avoid diluting good fronts. " +
            "Works with both NSGA-II and NSGA-III. Falls back to any other individual when no same-rank partner exists.",
            "Geospiza", "Pairing Strategies")
    {
    }

    protected override Bitmap Icon => Resources.InBreedingStrategy;

    public override Guid ComponentGuid => new("A1B4C8D2-E5F6-4712-8934-0C1D2E3F4A5B");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        // No parameters — pairing is determined by ParetoRank assigned during selection.
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Pairing Strategy", "PS", "The pairing strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        DA.SetData(0, new RankAwarePairingStrategy());
    }
}
