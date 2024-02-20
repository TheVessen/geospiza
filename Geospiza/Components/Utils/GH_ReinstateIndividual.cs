using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza;

public class ReinstateIndividual : GH_Component
{
    /// <summary>
    /// Initializes a new instance of the ReinstateIndividual class.
    /// </summary>
    public ReinstateIndividual()
        : base("ReinstateIndividual", "RI",
            "Reinstate an individual",
            "Geospiza", "Utils")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individual", "I", "The individual to reinstate", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Reinstate", "R", "Reinstate the individual", GH_ParamAccess.item, false);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }
    private Individual individual;

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Declare a variable for the input
        GH_ObjectWrapper individualWrapper = new GH_ObjectWrapper();
        // If the input is not retrieved, return
        if (!DA.GetData(0, ref individualWrapper)) return;
        individual = individualWrapper.Value as Individual;
        bool reinstate = false;
        if (!DA.GetData(1, ref reinstate)) return;

        if (reinstate)
        {
            OnPingDocument().ScheduleSolution(10,  ScheduleCallback);
        }
    }
    
    void ScheduleCallback(GH_Document doc)
    {
        OnPingDocument().NewSolution(false);
        individual.Reinstate();
        ExpirePreview(false);
        ExpireSolution(false);
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.ReinstateIndividuum;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("6692F05D-9C4F-4B99-B4C5-CF1D91C7B639"); }
    }
}