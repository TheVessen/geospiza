using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Webcomponents;

public class GH_PopulationToJSON : GH_Component
{
    public GH_PopulationToJSON()
        : base("PopulationToJson", "PTJ",
            "Converts a population to a JSON string",
            "Geospiza", "Webcomponents")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    protected override Bitmap Icon =>
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        null;

    public override Guid ComponentGuid => new("EED60EC5-E7F1-46B1-ACAD-7667A2565C35");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Population", "P", "The population to convert to JSON", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("JSON", "J", "The JSON string", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var populationWrapper = new GH_ObjectWrapper();
        if (!DA.GetData(0, ref populationWrapper)) return;

        if (populationWrapper.Value is GeospizaCore.Core.Population population)
        {
            var json = population.ToJson();

            DA.SetData(0, json);
        }
    }
}