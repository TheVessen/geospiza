using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza.Comonents;

public class GeneSelector : GH_Component
{
    //REF "https://discourse.mcneel.com/t/gene-pool-component/59835/8"


    /// <summary>
    /// Initializes a new instance of the GeneSelector class.
    /// </summary>
    public GeneSelector()
        : base("GeneSelector", "GS",
            "Collects the genes for the evolutionary algorithm. It is also possible to search the document for gene parameters these need the prefix GP_",
            "Geospiza", "Utils")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("GeneParams", "GP",
            "The gene parameters this can be number sliders or a galapagos gene list", GH_ParamAccess.tree);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Genes", "GID", "The gene ids", GH_ParamAccess.list);
    }
    
    private List<string> docParams = new List<string>();

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var allGeneParams = this.Params.Input[0].Sources;
        var geneIds = new List<string>();
        
        if (geneIds.Count == 0 || geneIds.Count != allGeneParams.Count)
        {
            geneIds.Clear();
            foreach (var param in allGeneParams)
            {
                geneIds.Add(param.InstanceGuid.ToString());
            }
        }

        DA.SetDataList(0, geneIds);
    }


    public override GH_Exposure Exposure => GH_Exposure.primary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.GeneSelector;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("DCCF2B6C-6790-4610-821B-F26C2FC938C2"); }
    }
}