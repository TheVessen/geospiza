using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GeospizaManager.Utils;

public static class GeoHelpers
{
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