using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Geospiza.Lib;
using Geospiza.Lib.Helpers;
using Grasshopper.Kernel.Special;
using Rhino.UI.Controls;

namespace Geospiza
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
            pManager.AddMeshParameter("Mesh", "M", "The mesh to evaluate", GH_ParamAccess.item);
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

            
            var mesh = new Mesh();
            if (!DA.GetData(0, ref mesh)) return;
            
            MeshBody meshBody = new MeshBody(mesh);
            Helpers.SendRequest(meshBody);
        }

        private void ScheduleCallback(GH_Document doc)
        {
            this.ExpirePreview(false);
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