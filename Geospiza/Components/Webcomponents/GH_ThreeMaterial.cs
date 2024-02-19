﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Geospiza.Core;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class GH_ThreeMAterial : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_ThreeMAterial class.
    /// </summary>
    public GH_ThreeMAterial()
        : base("ThreeMaterial", "TM",
            "ThreeMaterial",
            "Geospiza", "Webcomponents")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddColourParameter("Color", "C", "Color to display", GH_ParamAccess.tree);
        pManager.AddNumberParameter("Metalness", "M", "Metalness value between 0 and 1", GH_ParamAccess.tree, 0.0);
        pManager.AddNumberParameter("Roughness", "R", "Roughness value between 0 and 1", GH_ParamAccess.tree, 0.5);
        pManager.AddNumberParameter("Opacity", "O", "Opacity value between 0 and 1", GH_ParamAccess.tree, 1.0);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Material", "M", "The material", GH_ParamAccess.tree);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Structure<GH_Colour> colorWrapper = null;
        DA.GetDataTree(0, out colorWrapper);
        GH_Structure<GH_Number> metalnessWrapper = null;
        DA.GetDataTree(1, out metalnessWrapper);
        GH_Structure<GH_Number> roughnessWrapper = null;
        DA.GetDataTree(2, out roughnessWrapper);
        GH_Structure<GH_Number> opacityWrapper = null;
        DA.GetDataTree(3, out opacityWrapper);
        
        // List<Color> allColors = colorWrapper.AllData(true)
        //     .OfType<GH_Colour>() // Ensure the item is of type GH_Colour.
        //     .Select(ghColour => ghColour.Value) // Access the Value property.
        //     .ToList();
        // List<double> allMetalness = metalnessWrapper.AllData(true)
        //     .OfType<GH_Number>() // Ensure the item is of type GH_Number.
        //     .Select(ghNumber => ghNumber.Value) // Access the Value property.
        //     .ToList();
        // List<double> allRoughness = roughnessWrapper.AllData(true)
        //     .OfType<GH_Number>() // Ensure the item is of type GH_Number.
        //     .Select(ghNumber => ghNumber.Value) // Access the Value property.
        //     .ToList();
        // List<double> allOpacity = opacityWrapper.AllData(true)
        //     .OfType<GH_Number>() // Ensure the item is of type GH_Number.
        //     .Select(ghNumber => ghNumber.Value) // Access the Value property.
        //     .ToList();
        
        // int firstListLength = allColors.Count;
        // bool areAllSameLengthOrOne = new List<int> { allMetalness.Count, allRoughness.Count, allOpacity.Count }
        //     .All(count => count == firstListLength || count == 1);
        // if (!areAllSameLengthOrOne) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All lists must be the same length or have a length of 1");

        DataTree<ThreeMaterial> materials = new DataTree<ThreeMaterial>();

        foreach (GH_Path path in colorWrapper.Paths)
        {
            List<GH_Colour> colorsInPath = colorWrapper.get_Branch(path).OfType<GH_Colour>().ToList();

            IList metalnessBranch = metalnessWrapper.get_Branch(path);
            List<double> allMetalness = metalnessBranch != null ? metalnessBranch.OfType<GH_Number>().Select(ghNumber => ghNumber.Value).ToList() : new List<double> { 0.0 };

            IList roughnessBranch = roughnessWrapper.get_Branch(path);
            List<double> allRoughness = roughnessBranch != null ? roughnessBranch.OfType<GH_Number>().Select(ghNumber => ghNumber.Value).ToList() : new List<double> { 0.5 };

            IList opacityBranch = opacityWrapper.get_Branch(path);
            List<double> allOpacity = opacityBranch != null ? opacityBranch.OfType<GH_Number>().Select(ghNumber => ghNumber.Value).ToList() : new List<double> { 1.0 };

            foreach (GH_Colour color in colorsInPath)
            {
                var material = new ThreeMaterial()
                {
                    Color = color.Value,
                    Metalness = allMetalness.Count == 1 ? allMetalness[0] : allMetalness[path.Indices[0]],
                    Roughness = allRoughness.Count == 1 ? allRoughness[0] : allRoughness[path.Indices[0]],
                    Opacity = allOpacity.Count == 1 ? allOpacity[0] : allOpacity[path.Indices[0]]
                };
                materials.Add(material, path);
            }
        }

        DA.SetDataTree(0, materials);
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
        get { return new Guid("C9409BAC-5DD4-4054-BE4C-FCCBD53B7BD3"); }
    }
}