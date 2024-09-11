using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaManager.Strategies;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza;

public class GH_SinglePointCrossover : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the SinglePointCrossover class.
    /// </summary>
    public GH_SinglePointCrossover()
        : base("SinglePointCrossover", "SinglePointCrossover",
            "Single Point Crossover is a genetic algorithm operation used for the new generation population." +
            " It is a method of combining the genetic information of two parents to generate new offspring.  " +
            "In Single Point Crossover, a point on the parent chromosomes is selected. " +
            "All data beyond that point in the first parent (Parent1) is swapped with all of the data before that point in the second parent (Parent2) and vice versa. " +
            "This results in two offspring, each carrying some genetic information from both parents.",
            "Geospiza", "CrossoverStrategies")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("CrossoverRate", "CR", "The crossover rate", GH_ParamAccess.item, 0.7);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("CrossoverStrategy", "CS", "The crossover strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double crossoverRate = 0;
        if (!DA.GetData(0, ref crossoverRate)) return;
        
        var crossover = new SinglePointCrossover(crossoverRate);
        DA.SetData(0, crossover);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.SinglePointCrosover;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("28BDC3CE-B8D8-4895-B165-4C442A197F05"); }
    }
}