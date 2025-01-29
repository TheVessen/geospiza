using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Utils;

public class GH_Individual : GH_Component
{
    /// <summary>
    /// Initializes a new instance of the Individual class.
    /// </summary>
    public GH_Individual()
        : base("Individual", "I",
            "Extracts fitness value and genes from a Geospiza individual",
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
        pManager.AddGenericParameter("Genes", "G", "The genes", GH_ParamAccess.list);
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
        DA.SetDataList(1, individual.GenePool);
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.Individual;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("16A3E3D3-A282-460B-92CC-78C6EF91B8CC");
}