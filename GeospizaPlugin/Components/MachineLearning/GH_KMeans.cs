using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Accord.MachineLearning;
using GeospizaCore.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.MachineLearning;

/// <summary>
///     K-Means clustering for partitioning individuals by their gene vectors.
///     Each gene dimension is normalised to [0, 1) by its tick count so genes
///     with different ranges contribute equally to the distance metric.
///     Reference: J. B. MacQueen, "Some methods for classification and analysis of
///     multivariate observations," in Proceedings of the Fifth Berkeley Symposium on
///     Mathematical Statistics and Probability, vol. 1, pp. 281–297, 1967.
/// </summary>
public class GH_KMeans : GH_Component
{
    public GH_KMeans()
        : base("K-Means", "K-Means",
            "Clusters individuals using the K-Means algorithm",
            "Geospiza", "Machine Learning")
    {
    }

    protected override Bitmap Icon => null;

    public override Guid ComponentGuid => new("C736282F-693C-4B81-838A-4DF5A7A15FCE");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I", "Individuals to cluster", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Clusters", "C", "Number of clusters to generate", GH_ParamAccess.item, 2);
        pManager.AddBooleanParameter("Activate", "A", "Run the clustering", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Clusters", "C", "Clustered individuals", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var wrappers = new List<GH_ObjectWrapper>();
        var k = 2;
        var activate = false;

        if (!DA.GetDataList(0, wrappers)) return;
        if (!DA.GetData(1, ref k)) return;
        if (!DA.GetData(2, ref activate)) return;

        if (!activate) return;

        var individuals = wrappers.Select(w => w.Value).OfType<Individual>().ToList();

        if (individuals.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No individuals found.");
            return;
        }

        if (k <= 0 || k > individuals.Count)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"Cluster count must be between 1 and the number of individuals ({individuals.Count}).");
            return;
        }

        var vectors = individuals.Select(ToNormalisedVector).ToArray();
        var labels = new KMeans(k).Learn(vectors).Decide(vectors);

        var tree = new GH_Structure<GH_ObjectWrapper>();
        for (var i = 0; i < labels.Length; i++)
            tree.Append(new GH_ObjectWrapper(individuals[i]), new GH_Path(labels[i]));

        DA.SetDataTree(0, tree);
    }

    /// <summary>
    ///     Returns each gene's tick value normalised by its tick count, giving a value in [0, 1) per dimension.
    ///     Normalisation ensures genes with larger tick ranges don't dominate the Euclidean distance.
    /// </summary>
    private static double[] ToNormalisedVector(Individual individual)
    {
        return individual.GenePool
            .Select(g => g.TickCount > 0 ? (double)g.TickValue / g.TickCount : 0.0)
            .ToArray();
    }
}