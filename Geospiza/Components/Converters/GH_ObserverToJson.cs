using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaManager.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza;

public class GH_ObserverToJson : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_ObserverToJson class.
    /// </summary>
    public GH_ObserverToJson()
        : base("ObserverToJson", "ObserverToJson",
            "Converts an observer to a JSON string",
            "Geospiza", "Converter")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The observer to convert to JSON", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The observer as a JSON string", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Get inputs
        GH_ObjectWrapper observer = null;
        if (!DA.GetData(0, ref observer)) return;
        var observerType = observer.ScriptVariable() as EvolutionObserver;

        // Convert the observer to a JSON string
        string json = observerType.ToJson();

        // Set the output
        DA.SetData(0, json);
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
        get { return new Guid("D5FA6C30-850C-4188-BDB2-A3CC7B971275"); }
    }
}