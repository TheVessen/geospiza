using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Geospiza;
using Geospiza.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza.Comonents
{
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
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Geo", "G", "Geo to display", GH_ParamAccess.tree);
            pManager.AddGenericParameter("ThreeMaterial", "TM", "ThreeMaterial", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Listen", "L", "Listen for the solution", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Endpoint", "E", "The endpoint to send the solution to", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Solution", "S", "The solution", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_Goo> GHBaseGeo = new GH_Structure<IGH_Goo>();
            if (!DA.GetDataTree(0, out GHBaseGeo)) return;
            //Base params
            MeshingParameters mParams = new MeshingParameters();
            mParams.SimplePlanes = true;
            //vars
            List<Mesh> meshes = new List<Mesh>();
            List<ThreeMaterial> threeMaterials = new List<ThreeMaterial>();
            
            //Convert to ThreeMaterial
            GH_Structure<IGH_Goo> materialWrappers = new GH_Structure<IGH_Goo>();
            DA.GetDataTree(1, out materialWrappers);

            var listen = false;
            if (!DA.GetData(2, ref listen)) return;
            
            var endpoint = "";
            if (!DA.GetData(3, ref endpoint)) return;
            
            
            bool isValid = false;

            if (GHBaseGeo.get_Branch(0) != null && materialWrappers.get_Branch(0) != null)
            {
                // Check if both have the same structure
                
                var sameCount = GHBaseGeo.AllData(false).ToList().Count == materialWrappers.AllData(false).ToList().Count();
                if(sameCount || materialWrappers.AllData(false).ToList().Count == 1)
                {
                    isValid = true;
                }

                // If not the same structure, check if materialWrappers contains only one object
                if (!isValid)
                {
                    isValid = materialWrappers.AllData(true).Count() == 1;
                }
            }

            if (!isValid)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input data is not valid. The structure of the material and the geo must be the same or the material must contain only one object");
                return;
            }
            
            //Convert to mesh
            foreach (IGH_Goo goo in GHBaseGeo.AllData(true))
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
                            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not convert geo to mesh");
                            break;
                    }
                }
                catch
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong in the casting process");
                }
                
                if (meshes.Count == 0)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No meshes to convert");
                }
            }
            
            foreach (var threeMaterialWrapper in materialWrappers.AllData(false))
            {
                object internalData = threeMaterialWrapper.ScriptVariable();
                if (internalData is ThreeMaterial threeMaterial)
                {
                    threeMaterials.Add(threeMaterial);
                }
            }

            var meshBodies = new List<WebIndividual>();
           for (var i = 0; i < meshes.Count; i++)
            {
                var mesh = meshes[i];
                ThreeMaterial threeMaterial;
                if (threeMaterials.Count == 1)
                {
                    threeMaterial = threeMaterials[0];
                }
                else
                {
                    threeMaterial = threeMaterials[i];
                    
                }
                meshBodies.Add(new WebIndividual(mesh, threeMaterial));
            }
            
            if (endpoint == "")
            {
                endpoint = "http://127.0.0.1:5173/api/geokernel";
            }
            
            if (listen)
            {
                Helpers.SendRequest(meshBodies, endpoint);
            }
        }


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
        public override Guid ComponentGuid => new Guid("819d7e1b-ee19-49e1-9116-43156f5e0ce9");
    }
}