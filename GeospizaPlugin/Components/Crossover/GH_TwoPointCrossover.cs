using System;
using System.Drawing;
using GeospizaCore.Strategies;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Crossover;

public class GH_TwoPointCrossover : GH_Component
{
    /// <summary>
    /// Initializes a new instance of the TwoPointCrossover class.
    /// </summary>
    public GH_TwoPointCrossover()
        : base("Two Point Crossover", "TPC",
            "Two-point crossover is a genetic algorithm operation that generates new offspring by combining the genetic information of two parents." +
            " It selects two points on the parent chromosomes and swaps the data between these points from both parents, resulting in two offspring with mixed genetic information.",
            "Geospiza", "CrossoverStrategies")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Crossover Rate", "CR", "The crossover rate", GH_ParamAccess.item, 0.7);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Crossover Strategy", "CS", "The crossover strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double crossoverRate = 0;
        if (!DA.GetData(0, ref crossoverRate)) return;

        var crossover = new TwoPointCrossover(crossoverRate);
        DA.SetData(0, crossover);
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.TwoPointCrosover;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("3560D235-F910-4BC0-9A10-28CBCF6C3B81");
}