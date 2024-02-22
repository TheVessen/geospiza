using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Selection;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Components.Selection;

public class GH_StochasticUniversalSampling : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_StochasticUniversalSampling class.
    /// </summary>
    public GH_StochasticUniversalSampling()
        : base("StochasticUniversalSampling", "SUS",
            "Performs a Stochastic Universal Sampling. In SUS, the fitness of each individual is used " +
            "to assign a probability of selection. However, instead of selecting individuals " +
            "one at a time, SUS selects all individuals at once by spreading out evenly spaced pointers over the population's",
            "Geospiza", "SelectionStrategy")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("SelectionStrategy", "SS", "The selection strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var selection = new StochasticUniversalSampling();
        DA.SetData(0, selection);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.StochasticUniversalSampling;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("03256F11-0CF3-4547-986A-F16EBC2CC6A5"); }
    }
}