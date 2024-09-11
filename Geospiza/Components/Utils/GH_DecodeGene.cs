using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaManager.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza;

public class GH_DecodeGene : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_DecodeGene class.
    /// </summary>
    public GH_DecodeGene()
        : base("DecodeGene", "GH_DecodeGene",
            "Decodes the gene",
            "Geospiza", "Utils")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Gene", "G", "The gene to decode", GH_ParamAccess.list);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("GeneName", "GN", "The name of the gene", GH_ParamAccess.list);
        pManager.AddNumberParameter("GeneValue", "GV", "The value of the gene", GH_ParamAccess.list);
        pManager.AddTextParameter("GeneGuid", "GG", "The guid of the gene", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<Gene> genes = new List<Gene>();
        DA.GetDataList(0, genes);

        var names = new List<string>();
        var values = new List<double>();
        var guid = new List<string>();

        foreach (var gene in genes)
        {
            if (gene == null) continue;
            var name = gene.GeneName;
            if (gene.GenePoolIndex != -1)
            {
                name = name + "_" + gene.GenePoolIndex;
            }
            names.Add(name);
            values.Add(gene.TickValue);
            guid.Add(gene.GhInstanceGuid.ToString());
        }
        
        DA.SetDataList(0, names);
        DA.SetDataList(1, values);
        DA.SetDataList(2, guid);
    }
    
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.DeconstructGene;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("BAE4D73B-B25C-4A95-95D2-36BAED583297"); }
    }
}