﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents.Selection;

public class TournamentSelection : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the TournamentSelection class.
    /// </summary>
    public TournamentSelection()
        : base("TournamentSelection", "TS",
            "Tournament selection for the evolutionary algorithm. ",
            "Geospiza", "SelectionStrategy")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("TournamentSize", "TS", "The size of the tournament", GH_ParamAccess.item, 5);
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
        double tournamentSize = 0;
        double selectionSize = 0;
        if (!DA.GetData(0, ref tournamentSize)) return;
        if (!DA.GetData(1, ref selectionSize)) return;
        
        var tournamentSizeInt = Convert.ToInt32(tournamentSize);
        var selectionSizeInt = Convert.ToInt32(selectionSize);
        
        var selection = new Strategies.Selection.TournamentSelection(tournamentSizeInt, selectionSizeInt);
        
        DA.SetData(0, selection);
    }
    
    public override GH_Exposure Exposure => GH_Exposure.primary;

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
        get { return new Guid("BFD3F3A2-8FBE-4FE0-A392-6E02E9402F43"); }
    }
}