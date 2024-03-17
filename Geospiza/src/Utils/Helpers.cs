using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace Geospiza;

public static class Helpers
{
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

  public static List<Mesh> MeshConverter(GH_Structure<IGH_Goo> meshStruct)
  {
    var meshes = new List<Mesh>();
    var mParams = new MeshingParameters();
    mParams.SimplePlanes = true;
    foreach (var goo in meshStruct.AllData(true))
    {
      try
      {
        var internalData = goo.ScriptVariable(); // This method gets the underlying geometry data.

        switch (internalData)
        {
          case Mesh mesh:
            meshes.Add(mesh);
            break;

          case Brep brep:
            var brepMeshes = Mesh.CreateFromBrep(brep, mParams);
            var joinedMesh = new Mesh();
            foreach (var brepMesh in brepMeshes) joinedMesh.Append(brepMesh);

            meshes.Add(joinedMesh);
            break;

          case Surface surface:
            var surfaceMesh = Mesh.CreateFromSurface(surface, mParams);
            meshes.Add(surfaceMesh);
            break;

          default:
            throw new Exception("Could not convert geo to mesh");
        }
      }
      catch
      {
        throw new Exception("Something went wrong in the casting process");
      }

      if (meshes.Count == 0) throw new Exception("No meshes to convert");
    }

    return meshes;
  }
}