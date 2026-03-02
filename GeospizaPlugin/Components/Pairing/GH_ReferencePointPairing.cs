using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Pairing;

public class GH_ReferencePointPairing : GH_Component
{
    public GH_ReferencePointPairing()
        : base("Reference Point Pairing", "RPP",
            "Pairs individuals that share the same NSGA-III reference point niche. " +
            "Promotes exploitation within a niche while preserving diversity across fronts. " +
            "Best used with the NSGA-III solver. Falls back to random pairing when no niche partner exists.",
            "Geospiza", "Pairing Strategies")
    {
    }

    protected override Bitmap Icon => Resources.InBreedingStrategy;

    public override Guid ComponentGuid => new("3F7C2A91-D4E8-4B56-9F1A-6E3C0B8D5A72");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        // No parameters — strategy is fully determined by ReferencePointIndex on each individual.
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Pairing Strategy", "PS", "The pairing strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        DA.SetData(0, new ReferencePointPairingStrategy());
    }
}