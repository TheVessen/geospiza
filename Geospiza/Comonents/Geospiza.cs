using System;
using System.Collections.Generic;
using System.Linq;
using Geospiza.Lib;
using Geospiza.Lib.Helpers;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Geospiza.Comonents
{
    public class Geospiza : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Geospiza()
            : base("Geospiza Solver", "GS",
                "Genetic solver",
                "Geospiza", "Solvers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "The mesh to evaluate", GH_ParamAccess.tree);
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

        private int doneItterations = 0;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var meshStruct = new GH_Structure<GH_Mesh>();
            if (!DA.GetDataTree(0, out meshStruct)) return;

            var ghMeshList = meshStruct.FlattenData();

           var meshList = ghMeshList.Select(x => x.Value).ToList();

            var listen = false;
            if (!DA.GetData(1, ref listen)) return;
            
            var endpoint = "";
            if (!DA.GetData(2, ref endpoint)) return;
            
            
            var meshBodies = meshList.Select(x => new MeshBody(x)).ToList();
            
            if (endpoint == "")
            {
                endpoint = "http://127.0.0.1:5173/api/geokernel";
            }
            

            if (listen)
            {
                Helpers.SendRequest(meshBodies, endpoint);
            }
        }


        public List<double> Solution { get; set; } = new List<double>();

        /// <summary>
        /// ProviIdes an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("819d7e1b-ee19-49e1-9116-43156f5e0ce9");
    }
}