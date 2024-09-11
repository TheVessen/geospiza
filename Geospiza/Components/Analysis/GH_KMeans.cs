﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Microsoft.ML;
using Microsoft.ML.Data;
using Accord.MachineLearning;
using GeospizaManager.Core;

namespace Geospiza;

public class GH_KMeans : GH_Component
{
    
    public class DataPoint
    {
        [VectorType]
        public float[] Features { get; set; }
    }

    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("Score")]
        public float[] Distances { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the GH_KMeans class.
    /// </summary>
    public GH_KMeans()
        : base("K-Means", "K-Means",
            "Description",
            "Geospiza", "Analysis")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I", "Individuals to cluster", GH_ParamAccess.list);
        pManager.AddNumberParameter("Clusters", "C", "Number of clusters to generate", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Activate", "A", "Activate the component", GH_ParamAccess.item, false);
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
            int[] labels = clusters.Decide(vectors);
        
        
            // Create a new DataTree
            var tree = new GH_Structure<GH_ObjectWrapper>();

            for (int i = 0; i < labels.Length; i++)
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
    
    protected static double[][] IndividualToVector(List<Individual> individuals)
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
    protected override Bitmap Icon
    {
        get
        {
            //You can add image files to your project resources and access them like this:
            // return Resources.IconForThisComponent;
            return null;
        }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("C736282F-693C-4B81-838A-4DF5A7A15FCE"); }
    }
}