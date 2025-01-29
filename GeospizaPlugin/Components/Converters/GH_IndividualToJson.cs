using System;
using System.Drawing;
using GeospizaCore.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Converters;

public class IndividualToJson : GH_Component
{
    /// <summary>
    /// Initializes a new instance of the IndividualToJson class.
    /// </summary>
    public IndividualToJson()
        : base("Individual To Json", "IToJ",
            "Converts an individual to a JSON string",
            "Geospiza", "Converter")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individual", "I", "The individual to convert to JSON", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var individualWrapper = new GH_ObjectWrapper();
        if (!DA.GetData(0, ref individualWrapper)) return;

        if (individualWrapper.Value is Individual individual)
        {
            var json = individual.ToJson();

            DA.SetData(0, json);
        }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.IndividualToJSON;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("FDB78846-7982-42E4-B8ED-EF37AC136612");
}