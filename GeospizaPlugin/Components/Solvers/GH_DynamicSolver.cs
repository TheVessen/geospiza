// //Looks promising, but not working yet :) Doc will follow
//
// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Linq;
// using GeospizaCore.Compute;
// using GeospizaCore.Solvers;
// using Grasshopper.Kernel;
// using Grasshopper.Kernel.Types;
//
// namespace GeospizaPlugin.Components.Solvers;
//
// public class GH_DynamicSolver : GH_Component
// {
//   /// <summary>
//   /// Initializes a new instance of the GH_DynamicSolver class.
//   /// </summary>
//   public GH_DynamicSolver()
//     : base("Dynamic Solver", "Dynamic Solver",
//       "Solves multiple genetic algorithms in parallel with different settings",
//       "Geospiza", "Solvers")
//   {
//   }
//
//   public override GH_Exposure Exposure => GH_Exposure.hidden;
//
//   /// <summary>
//   /// Registers all the input parameters for this component.
//   /// </summary>
//   protected override void RegisterInputParams(GH_InputParamManager pManager)
//   {
//     pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
//       GH_ParamAccess.list);
//     pManager.AddTextParameter("File Path", "FP", "The file path to grasshopper filed that should be solved",
//       GH_ParamAccess.item);
//     pManager.AddBooleanParameter("Run", "R", "Run the solver for running locally", GH_ParamAccess.item, false);
//   }
//
//   /// <summary>
//   /// Registers all the output parameters for this component.
//   /// </summary>
//   protected override void RegisterOutputParams(GH_OutputParamManager pManager)
//   {
//     // pManager.AddGenericParameter("Obervers", "O", "The observers for the evolutionary algorithms", GH_ParamAccess.list);
//     pManager.AddTextParameter("Results", "R", "The results of the evolutionary algorithms", GH_ParamAccess.item);
//   }
//
//   /// <summary>
//   /// This is the method that actually does the work.
//   /// </summary>
//   /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//   protected override void SolveInstance(IGH_DataAccess DA)
//   {
//     // Variables
//     List<GH_ObjectWrapper> settingsWrappers = new();
//     var filePath = "";
//     var run = false;
//
//     // Set variables
//     DA.GetDataList(0, settingsWrappers);
//     DA.GetData(1, ref filePath);
//     DA.GetData(2, ref run);
//
//     var settings = settingsWrappers.Select(s => s.Value).Cast<SolverSettings>().ToList();
//
//     if (settings.Count == 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No settings provided");
//
//     ComputeServer.ApiKey = "API";
//     ComputeServer.AuthToken = "TOKEN";
//     ComputeServer.WebAddress = "http://localhost:6500";
//
//     var trees = new List<GrasshopperDataTree>();
//     var random = new Random();
//     var randomNumber = new GrasshopperObject(random.Next(0, 1000));
//     var trigger = new GrasshopperDataTree("trigger");
//     trigger.Add("0", new List<GrasshopperObject> { randomNumber });
//     trees.Add(trigger);
//
//     var settingsTree = new GrasshopperDataTree("settings");
//     var settingsJson = settings.Select(s => new GrasshopperObject(s.ToJson())).ToList();
//     settingsTree.Add("0", settingsJson);
//     trees.Add(settingsTree);
//
//     //Call the server
//     var result = GrasshopperCompute.EvaluateDefinition(filePath, trees);
//
//     //Get fitness
//     var fitness = result[0].InnerTree.First().Value[0].Data;
//
//     DA.SetData(0, fitness);
//   }
//
//   /// <summary>
//   /// Provides an Icon for the component.
//   /// </summary>
//   protected override Bitmap Icon =>
//     //You can add image files to your project resources and access them like this:
//     // return Resources.IconForThisComponent;
//     null;
//
//   /// <summary>
//   /// Gets the unique ID for this component. Do not change this ID after release.
//   /// </summary>
//   public override Guid ComponentGuid => new("866F5DF3-2C87-4882-81B7-68F9380E316A");
// }