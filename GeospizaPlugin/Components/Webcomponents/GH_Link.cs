using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using GeospizaCore.Web;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;

namespace GeospizaPlugin.Components.Webcomponents;

public class GeoLink : GH_Component
{
  /// <summary>
  /// Each implementation of GH_Component must provide a public 
  /// constructor without any arguments.
  /// Category represents the Tab in which the component will appear, 
  /// Subcategory the panel. If you use non-existing tab or panel names, 
  /// new tabs/panels will automatically be created.
  /// </summary>
  public GeoLink()
    : base("Geospiza Link", "GL",
      "Sends live data to a web api",
      "Geospiza", "Webcomponents")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("WebGeo", "WG", "Geo to display", GH_ParamAccess.tree);
    pManager.AddGenericParameter("Additional Data", "AD", "Additional data to send", GH_ParamAccess.tree);
    pManager.AddTextParameter("Endpoint", "E",
      "The endpoint to send the solution to eg. http://127.0.0.1:5173/api/someEndpoint ", GH_ParamAccess.item);
    pManager.AddBooleanParameter("Listen", "L", "Listen for the solution", GH_ParamAccess.item, false);

    pManager[3].Optional = true;
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
  /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
  /// to store data in output parameters.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var meshBodiesStruct = new GH_Structure<IGH_Goo>();
    if (!DA.GetDataTree(0, out meshBodiesStruct)) return;
    var meshBodiesWrapper = meshBodiesStruct.AllData(false).ToList();
    var meshBodies = new List<WebIndividual>();

    foreach (var meshBodyWrapper in meshBodiesWrapper)
    {
      var internalData = meshBodyWrapper.ScriptVariable();
      if (internalData is WebIndividual individual) meshBodies.Add(individual);
    }

    var additionalDataWrappers = new GH_Structure<IGH_Goo>();
    DA.GetDataTree(1, out additionalDataWrappers);

    var additionalData = new List<Tuple<string, string>>();

    if (additionalDataWrappers.AllData(false).Count() != 0)
      foreach (var additionalDataWrapper in additionalDataWrappers.AllData(false))
      {
        var internalData = additionalDataWrapper.ScriptVariable();
        if (internalData is Tuple<string, string> aditionalData) additionalData.Add(aditionalData);
      }

    var endpoint = "";
    if (!DA.GetData(2, ref endpoint)) return;

    var listen = false;
    if (!DA.GetData(3, ref listen)) return;

    if (listen) SendWebRequest(meshBodies, additionalData, endpoint, this);
  }

  public override GH_Exposure Exposure => GH_Exposure.primary;


  /// <summary>
  /// ProviIdes an Icon for every component that will be visible in the User Interface.
  /// Icons need to be 24x24 pixels.
  /// You can add image files to your project resources and access them like this:
  /// return Resources.IconForThisComponent;
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.Weblink;

  /// <summary>
  /// Each component must have a unique Guid to identify it. 
  /// It is vital this Guid doesn't change otherwise old ghx files 
  /// that use the old ID will partially fail during loading.
  /// </summary>
  public override Guid ComponentGuid => new("819d7e1b-ee19-49e1-9116-43156f5e0ce9");

  public static void SendWebRequest(List<WebIndividual> dataList, List<Tuple<string, string>> additionalData,
    string endpoint, GH_Component component)
  {
    var meshes = dataList.Select(individual => individual.ToAnonymousObject()).ToList();

    var rootObject = new Dictionary<string, object>
    {
      { "Meshes", meshes }
    };

    foreach (var data in additionalData) rootObject.Add(data.Item1, data.Item2);

    var json = JsonConvert.SerializeObject(rootObject);


    using (var client = new HttpClient())
    {
      HttpResponseMessage response = null;

      try
      {
        response = client.GetAsync(endpoint).Result;
      }
      catch (Exception ex)
      {
        component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Error: {ex.Message}");
        return;
      }

      if (!response.IsSuccessStatusCode)
      {
        component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "API endpoint is not online.");
        return;
      }

      var content = new StringContent(json, Encoding.UTF8, "application/json");

      var result = client.PostAsync(endpoint, content).Result;

      if (result.IsSuccessStatusCode)
      {
        // Handle success
      }
      // Handle failure
    }
  }
}