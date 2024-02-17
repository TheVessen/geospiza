using System;
using System.Collections.Generic;
using System.Drawing;
using Geospiza.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza;

public class GH_Observer : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the Observer class.
    /// </summary>
    public GH_Observer()
        : base("Observer", "Observer",
            "Description",
            "Geospiza", "Subcategory")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "P", "The population to observe", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I", "The individuals in the population", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper wrapper = null;

        // Reference the input
        if (!DA.GetData(0, ref wrapper)) return;
        
        if(wrapper.Value is Observer)
        {
            var obs = (Observer)wrapper.Value;
            if (obs.CurrentPopulation == null || obs.CurrentPopulation.Count == 0)
            {
                return;
            }
  
            DA.SetDataList(0, obs.GetCurrentPopulation().Inhabitants);

        }
        else
        {
            throw new Exception("The input is not a population");
        }

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
        get { return new Guid("2D368D5B-DC50-432D-85DD-311435EF865C"); }
    }
}