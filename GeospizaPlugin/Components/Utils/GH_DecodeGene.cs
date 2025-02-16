﻿using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Utils;

public class GH_DecodeGene : GH_Component
{
    /// <summary>
    ///     Initializes a new instance of the GH_DecodeGene class.
    /// </summary>
    public GH_DecodeGene()
        : base("Decode Gene", "Decode Gene",
            "Decodes a gene into its name, value and unique identifier",
            "Geospiza", "Utils")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Resources.DeconstructGene;

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("BAE4D73B-B25C-4A95-95D2-36BAED583297");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Gene", "G", "The gene to decode", GH_ParamAccess.list);
    }

    /// <summary>
    ///     Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Gene Name", "GN", "The name of the gene", GH_ParamAccess.list);
        pManager.AddNumberParameter("Gene Value", "GV", "The value of the gene", GH_ParamAccess.list);
        pManager.AddTextParameter("Gene Guid", "GG", "The guid of the gene", GH_ParamAccess.list);
    }

    /// <summary>
    ///     This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var genes = new List<Gene>();
        DA.GetDataList(0, genes);

        var names = new List<string>();
        var values = new List<double>();
        var guid = new List<string>();

        foreach (var gene in genes)
        {
            if (gene == null) continue;
            var name = gene.GeneName;
            if (gene.GenePoolIndex != -1) name = name + "_" + gene.GenePoolIndex;
            names.Add(name);
            values.Add(gene.TickValue);
            guid.Add(gene.GhInstanceGuid.ToString());
        }

        DA.SetDataList(0, names);
        DA.SetDataList(1, values);
        DA.SetDataList(2, guid);
    }
}