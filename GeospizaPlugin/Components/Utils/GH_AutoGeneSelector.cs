using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GeospizaPlugin.Components.Utils;

public class GH_AutoGeneSelector : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_AutoGeneSelector class.
  /// </summary>
  public GH_AutoGeneSelector()
    : base("Auto Gene Selector", "AutoGene",
      "Automatically finds and collects gene parameters from Gene Pool components and sliders prefixed with 'GP_'",
      "Geospiza", "Utils")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddBooleanParameter("Clear", "C", "Clear the gene parameters from the doc search", GH_ParamAccess.item,
      false);
    pManager.AddBooleanParameter("SearchDocument", "SD", "Search the document for gene parameters",
      GH_ParamAccess.item, false);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddTextParameter("Genes", "GID", "The gene ids", GH_ParamAccess.list);
  }

  private List<string> docParams = new();

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var geneIds = new List<string>();
    var searchDocument = false;
    DA.GetData(1, ref searchDocument);
    var clear = false;
    DA.GetData(0, ref clear);


    if (searchDocument)
      foreach (var ghobject in OnPingDocument().Objects)
      {
        var currentType = ghobject.GetType().ToString();

        var isRightType = currentType == "Grasshopper.Kernel.Special.GH_NumberSlider" ||
                          currentType == "GalapagosComponents.GalapagosGeneListObject";
        if (ghobject.NickName.StartsWith("GP_") && isRightType)
          if (!docParams.Contains(ghobject.InstanceGuid.ToString()))
            docParams.Add(ghobject.InstanceGuid.ToString());
      }

    if (clear) docParams.Clear();

    geneIds.AddRange(docParams);
    DA.SetDataList(0, geneIds);
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon =>
    //You can add image files to your project resources and access them like this:
    // return Resources.IconForThisComponent;
    null;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("CC1BA854-CDE4-4A88-BFE7-97105DD75F9B");
}