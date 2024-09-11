﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Geospiza.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza;

public class GH_WebGeo : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_DisplayObject class.
    /// </summary>
    public GH_WebGeo()
        : base("WebIndividual", "WI",
            "Collects data to be displayed in the web output",
            "Geospiza", "Webcomponents")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Geo", "G", "Geo to display", GH_ParamAccess.tree);
        pManager.AddGenericParameter("ThreeMaterial", "TM", "ThreeMaterial", GH_ParamAccess.tree);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("WebGeo", "WG", "The display object", GH_ParamAccess.list);
    }
    
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Structure<IGH_Goo> GHBaseGeo = new GH_Structure<IGH_Goo>();
        if (!DA.GetDataTree(0, out GHBaseGeo)) return;
        //Base params
        MeshingParameters mParams = new MeshingParameters();
        mParams.SimplePlanes = true;
        //vars
        List<ThreeMaterial> threeMaterials = new List<ThreeMaterial>();

        //Convert to ThreeMaterial
        GH_Structure<IGH_Goo> materialWrappers = new GH_Structure<IGH_Goo>();
        DA.GetDataTree(1, out materialWrappers);
        
        bool isValid = false;

        if (GHBaseGeo.get_Branch(0) != null && materialWrappers.get_Branch(0) != null)
        {
            // Check if both have the same structure

            var sameCount = GHBaseGeo.AllData(false).ToList().Count ==
                            materialWrappers.AllData(false).ToList().Count();
            if (sameCount || materialWrappers.AllData(false).ToList().Count == 1)
            {
                isValid = true;
            }

            // If not the same structure, check if materialWrappers contains only one object
            if (!isValid)
            {
                isValid = materialWrappers.AllData(true).Count() == 1;
            }
        }

        if (!isValid)
        {
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "The input data is not valid. The structure of the material and the geo must be the same or the material must contain only one object");
            return;
        }

        List<Mesh> meshes = Helpers.MeshConverter(GHBaseGeo);

        foreach (var threeMaterialWrapper in materialWrappers.AllData(false))
        {
            object internalData = threeMaterialWrapper.ScriptVariable();
            if (internalData is ThreeMaterial threeMaterial)
            {
                threeMaterials.Add(threeMaterial);
            }
        }
        
        var meshBodies = new List<WebIndividual>();
        for (var i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];
            ThreeMaterial threeMaterial;
            if (threeMaterials.Count == 1)
            {
                threeMaterial = threeMaterials[0];
            }
            else
            {
                threeMaterial = threeMaterials[i];
            }

            meshBodies.Add(new WebIndividual(mesh, threeMaterial));
        }
        
        DA.SetDataList(0, meshBodies);
        
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.WebGeo;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("DD37E066-9E47-4C3C-82A0-0C64141F22B4"); }
    }
}