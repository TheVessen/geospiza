using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Selection;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza;


/// <summary>
/// 
/// </summary>
public class GH_ExclusiveSelection : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_ExclusiveSelection class.
    /// </summary>
    public GH_ExclusiveSelection()
        : base("ExclusiveSelection", "ES",
            "Performs an exclusive selection",
            "Geospiza", "SelectionStrategy")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("TopPercentage", "TP", "The percentage of the top individuals to be selected", GH_ParamAccess.item, 0.1);
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
        double topPercentage = 0;
        if (!DA.GetData(0, ref topPercentage)) return;
        
        var selection = new ExclusiveSelection(topPercentage);
        
        DA.SetData(0, selection);
    }
    
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon
    {
        get
        {
            //You can add image files to your project resources and access them like this:
            // return Resources.IconForThisComponent;
            return null;
        }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("D854EADC-0573-4FA9-BFD2-F0C0B9387D40"); }
    }
}