﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Selection;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza;

public class GH_IsotropicSelection : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_IsotropicSelection class.
    /// </summary>
    public GH_IsotropicSelection()
        : base("IsotropicSelection", "IS",
            "Description",
            "Geospiza", "SelectionStrategy")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("SelectionSize", "SS", "The size of the selection", GH_ParamAccess.item, 2);
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
        double selectionSize = 0;
        if (!DA.GetData(0, ref selectionSize)) return;
        
        var selectionSizeInt = Convert.ToInt32(selectionSize);
        
        var selection = new IsotropicSelection(selectionSizeInt);
        
        DA.SetData(0, selection);
    }

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
        get { return new Guid("3CDB0B8B-17A7-4A47-A096-7AAE24143F66"); }
    }
}