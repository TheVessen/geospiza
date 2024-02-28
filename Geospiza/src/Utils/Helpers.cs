using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace Geospiza
{
    public static class Helpers
    {
        public static void SendRequest(List<WebIndividual> dataList, List<Tuple<string,string>> additionalData, string endpoint)
        {
            var meshes = dataList.Select(individual => individual.ToAnonymousObject()).ToList();

            var rootObject = new Dictionary<string, object>
            {
                { "Meshes", meshes }
            };

            foreach (var data in additionalData)
            {
                rootObject.Add(data.Item1, data.Item2);
            }

            var json = JsonConvert.SerializeObject(rootObject);
            

            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var result = client.PostAsync(endpoint, content).Result;

                if (result.IsSuccessStatusCode)
                {
                    // Handle success
                }
                else
                {
                    // Handle failure
                }
            }
        }

        public static List<Mesh> MeshConverter(GH_Structure<IGH_Goo> meshStruct)
        {
            var meshes = new List<Mesh>();
            MeshingParameters mParams = new MeshingParameters();
            mParams.SimplePlanes = true;
            foreach (IGH_Goo goo in meshStruct.AllData(true))
            {
                try
                {
                    object internalData = goo.ScriptVariable(); // This method gets the underlying geometry data.

                    switch (internalData)
                    {
                        case Mesh mesh:
                            meshes.Add(mesh);
                            break;

                        case Brep brep:
                            Mesh[] brepMeshes = Mesh.CreateFromBrep(brep, mParams);
                            var joinedMesh = new Mesh();
                            foreach (var brepMesh in brepMeshes)
                            {
                                joinedMesh.Append(brepMesh);
                            }

                            meshes.Add(joinedMesh);
                            break;

                        case Surface surface:
                            Mesh surfaceMesh = Mesh.CreateFromSurface(surface, mParams);
                            meshes.Add(surfaceMesh);
                            break;

                        default:
                            throw new System.Exception("Could not convert geo to mesh");
                    }
                }
                catch
                {
                    throw new System.Exception("Something went wrong in the casting process");
                }

                if (meshes.Count == 0)
                {
                    throw new System.Exception("No meshes to convert");
                }
                
            }

            return meshes;
        }
    }
};