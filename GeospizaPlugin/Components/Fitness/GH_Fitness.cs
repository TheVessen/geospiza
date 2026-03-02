using System;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Fitness;

public class GH_Fitness : GH_Component
{

    public GH_Fitness()
        : base("Fitness", "F",
            "Assigns a numerical fitness score that guides the evolutionary optimization process. " +
            "This component connects the fitness evaluation to Geospiza's main evolutionary solver.",
            "Geospiza", "Fitness")
    {
    }

    public double FitnessValue { get; set; } = 0;

    public override GH_Exposure Exposure => GH_Exposure.primary;


    protected override Bitmap Icon => Resources.Fitness;


    public override Guid ComponentGuid => new("FC2E37D7-CE42-4232-B1C8-07C81ADF75D7");


    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Fitness", "F", "Fitness value for the evolutionary algorithm.",
            GH_ParamAccess.item);
    }


    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }


    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double fitness = 0;
        if (!DA.GetData(0, ref fitness)) return;

        // Add the new fitness value
        GeospizaCore.Core.Fitness.Instance.SetFitness(fitness);
    }
}