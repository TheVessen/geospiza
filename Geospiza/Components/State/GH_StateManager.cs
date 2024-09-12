using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeospizaManager.Core;
using Grasshopper.Kernel;
using System.Drawing;
using Rhino.Geometry;

namespace Geospiza.Components;

public class GH_StateManager : GH_Component
{

    /// <summary>
    /// Initializes a new instance of the GH_StateManager class.
    /// </summary>
    public GH_StateManager()
        : base("StateManager", "SM",
            "Description",
            "Geospiza", "State")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Number", "N", "Description", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var num = 0.0;
        if (!DA.GetData(0, ref num)) return;
        
        var individual = new Individual();
        individual.SetGeneration(10);
        individual.SetFitness(num);
        var json = individual.ToJson();

        Task.Run(() => DataSender.SendDataAsync(json));
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
        get { return new Guid("C467D430-2667-440C-B95F-B93C2C26D667"); }
    }
}