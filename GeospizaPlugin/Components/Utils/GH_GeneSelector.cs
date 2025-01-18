using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Utils;

public class GH_GeneCollector : GH_Component
{
  //REF "https://discourse.mcneel.com/t/gene-pool-component/59835/8"
  
  /// <summary>
  /// Initializes a new instance of the GeneSelector class.
  /// </summary>
  public GH_GeneCollector()
    : base("Gene Collector", "GeneCollector",
      "Collects and identifies parameters for evolutionary optimization. Compatible with numeric sliders and Galapagos gene pools.",
      "Geospiza", "Utils")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("GeneParams", "GP",
      "The gene parameters this can be number sliders or a galapagos gene list", GH_ParamAccess.tree);
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
    var allGeneParams = Params.Input[0].Sources;
    var geneIds = new List<string>();

    if (geneIds.Count == 0 || geneIds.Count != allGeneParams.Count)
    {
      geneIds.Clear();
      foreach (var param in allGeneParams) geneIds.Add(param.InstanceGuid.ToString());
    }

    DA.SetDataList(0, geneIds);
  }


  public override GH_Exposure Exposure => GH_Exposure.primary;

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon => Properties.Resources.GeneSelector;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("DCCF2B6C-6790-4610-821B-F26C2FC938C2");
}