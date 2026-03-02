using System;
using System.Drawing;
using GeospizaCore.Strategies;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Termination;

public class GH_PopulationDiversity : GH_Component
{
    public GH_PopulationDiversity()
        : base("Population Diversity", "PD",
            "Terminate the genetic algorithm when the population diversity is below a certain threshold",
            "Geospiza", "Termination Strategies")
    {
    }

    protected override Bitmap Icon => Resources.PopulationDiversity;

    public override Guid ComponentGuid => new("636C470F-C794-4409-8347-B90D64166F74");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Threshold", "T", "The number of unique individuals needed for termination",
            GH_ParamAccess.item, 2);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Termination Strategy", "TS", "The termination strategy", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double threshold = 0;
        if (!DA.GetData(0, ref threshold)) return;
        DA.SetData(0, new PopulationDiversity(threshold));
    }
}