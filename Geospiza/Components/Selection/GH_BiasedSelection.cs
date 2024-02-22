using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Selection;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza;

public class GH_BiasedSelection : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_BiasedSelection class.
    /// </summary>
    public GH_BiasedSelection()
        : base("BiasedSelection", "BS",
            "Performs a biased selection. In Biased Selection, each individual in the population is assigned " +
            "a selection probability proportional to its fitness.Then, " +
            "a number of individuals are selected randomly based on these probabilities.",
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
        
        var selection = new BiasedSelection();
        
        DA.SetData(0, selection);
        
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.BiasSelection;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("2A14D98B-C387-4C60-8EAC-28D821DD7948"); }
    }
}