using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Pairing;

public class GH_InbreedingPairing : GH_Component
{
    public GH_InbreedingPairing()
        : base("Inbreeding Pairing", "IP",
            "This code is a part of a genetic algorithm that pairs individuals based on their genetic similarity or dissimilarity. " +
            "It uses an in-breeding factor to determine the preference for selecting mates.",
            "Geospiza", "Pairing Strategies")
    {
    }

    protected override Bitmap Icon => Resources.InBreedingStrategy;

    public override Guid ComponentGuid => new("86B5A4E3-080A-4419-A0AD-FD42CB4890F5");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("In Breeding Factor", "IBF",
            "Inbreeding Factor, a value between 0 and 1. A value of 0 promotes pairing between individuals with high genetic similarity, " +
            "while a value of 1 encourages pairing between individuals with high genetic dissimilarity.",
            GH_ParamAccess.item,
            0.2);
        pManager.AddNumberParameter("Distance Function", "DF",
            "The distance function to use. 0 for euclidean, 1 for manhattan", GH_ParamAccess.item, 1);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Pairing Strategy", "PS", "The pairing strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double inBreedingFactor = 0;
        if (!DA.GetData(0, ref inBreedingFactor)) return;
        double distanceFunction = 0;
        if (!DA.GetData(1, ref distanceFunction)) return;
        var distanceFunctionInt = Convert.ToInt32(distanceFunction);
        if (distanceFunctionInt is > 1 or < 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Distance function must be 0 or 1");

        DistanceFunctionType df;
        if (distanceFunction == 0)
            df = DistanceFunctionType.Euclidean;
        else
            df = DistanceFunctionType.Manhattan;

        var pairing = new PairingStrategy(inBreedingFactor, df);

        DA.SetData(0, pairing);
    }
}