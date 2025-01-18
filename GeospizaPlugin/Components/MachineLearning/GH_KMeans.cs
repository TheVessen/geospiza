using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Accord.MachineLearning;
using GeospizaManager.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Microsoft.ML.Data;

namespace GeospizaPlugin.Components.Analysis;

public class GH_KMeans : GH_Component
{
  /// <summary>
  /// Initializes a new instance of the GH_KMeans class.
  /// </summary>
  public GH_KMeans()
    : base("K-Means", "K-Means",
      "Clusters individuals using the K-Means algorithm",
      "Geospiza", "Machine Learning")
  {
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Individuals", "I", "Individuals to cluster", GH_ParamAccess.list);
    pManager.AddNumberParameter("Clusters", "C", "Number of clusters to generate", GH_ParamAccess.item);
    pManager.AddBooleanParameter("Activate", "A", "Run the clustering", GH_ParamAccess.item, false);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Clusters", "C", "Clustered individuals", GH_ParamAccess.tree);
  }

  /// <summary>
  /// This is the method that actually does the work.
  /// </summary>
  /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
  protected override void SolveInstance(IGH_DataAccess DA)
  {
    var individualWrapper = new List<GH_ObjectWrapper>();
    var numberOfClusters = 0.0;
    var activate = false;

    if (!DA.GetDataList(0, individualWrapper)) return;
    if (!DA.GetData(1, ref numberOfClusters)) return;
    if (!DA.GetData(2, ref activate)) return;

    if (activate)
    {
      var individuals = individualWrapper.Select(wrapper => wrapper.Value).OfType<Individual>().ToList();

      var vectors = IndividualToVector(individuals);
      var means = new KMeans((int)numberOfClusters);

      var clusters = means.Learn(vectors);

      // Use the clusters to predict group memberships:
      var labels = clusters.Decide(vectors);


      // Create a new DataTree
      var tree = new GH_Structure<GH_ObjectWrapper>();

      for (var i = 0; i < labels.Length; i++)
      {
        // Create a new path for each label
        var path = new GH_Path(labels[i]);

        // Add each individual to the corresponding path
        tree.Append(new GH_ObjectWrapper(individuals[i]), path);
      }

      // Set the DataTree as output
      DA.SetDataTree(0, tree);
    }
  }

  private static double[][] IndividualToVector(List<Individual> individuals)
  {
    var vectors = new List<List<double>>();
    foreach (var individual in individuals)
    {
      var vector = new List<double>();
      foreach (var gene in individual.GenePool)
      {
        double value = gene.TickValue;
        vector.Add(value);
      }

      vectors.Add(vector);
    }

    return vectors.Select(a => a.ToArray()).ToArray();
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
  public override Guid ComponentGuid => new("C736282F-693C-4B81-838A-4DF5A7A15FCE");
}