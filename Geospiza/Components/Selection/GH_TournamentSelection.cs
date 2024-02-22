using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Selection;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents.Selection;

public class GH_TournamentSelection : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the TournamentSelection class.
    /// </summary>
    public GH_TournamentSelection()
        : base("TournamentSelection", "TS",
            "Performs a tournament selection. In Tournament Selection, a subset of individuals is chosen " +
            "from the population, and the individual with the highest " +
            "fitness in this group is selected.The process is repeated until the desired number of individuals is selected.",
            "Geospiza", "SelectionStrategy")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("TournamentSize", "TS", "The size of the tournament", GH_ParamAccess.item, 4);
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
        if (!DA.GetData(0, ref tournamentSize)) return;
        
        var tournamentSizeInt = Convert.ToInt32(tournamentSize);
        var selection = new TournamentSelection(tournamentSizeInt);
        
        DA.SetData(0, selection);
    }
    
    public override GH_Exposure Exposure => GH_Exposure.primary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.TournamentSelection;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("BFD3F3A2-8FBE-4FE0-A392-6E02E9402F43"); }
    }
}