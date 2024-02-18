using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Geospiza.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza;

public class GH_Individual : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_Individual class.
    /// </summary>
    public GH_Individual()
        : base("Individual", "I",
            "Gets properties of the individual",
            "Geospiza", "Utils")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individual", "I", "The individual to reinstate", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("Fitness", "F", "The fitness value", GH_ParamAccess.item);
        pManager.AddNumberParameter("Ticks", "T", "The number of ticks", GH_ParamAccess.list);
        
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper individualWrapper = null;
        if (!DA.GetData(0, ref individualWrapper)) return;
        var individual = individualWrapper.Value as Individual;
        
        DA.SetData(0, individual.Fitness);
        DA.SetDataList(1,individual.GenePool.Select(gene => gene.TickValue), 1);
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

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
        get { return new Guid("16A3E3D3-A282-460B-92CC-78C6EF91B8CC"); }
    }
}