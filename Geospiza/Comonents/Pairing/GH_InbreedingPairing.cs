using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Strategies.Pairing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Geospiza.Comonents.Pairing;

public class GH_InbreedingPairing : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the InbreedingPairing class.
    /// </summary>
    public GH_InbreedingPairing()
        : base("InbreedingPairing", "IP",
            "Description",
            "Geospiza", "PairingStrategies")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("InBreedingFactor", "P", "Description", GH_ParamAccess.item, 0);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("PairingStrategy", "PS", "The pairing strategy", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        double inBreedingFactor = 0;
        if (!DA.GetData(0, ref inBreedingFactor)) return;
        
        var pairing = new InbreedingPairingStrategy(inBreedingFactor);
        
        DA.SetData(0, pairing);
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
        get { return new Guid("86B5A4E3-080A-4419-A0AD-FD42CB4890F5"); }
    }
}