using System;
using System.Drawing;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Population;

public class GH_Individual : GH_Component
{
    public GH_Individual()
        : base("Individual", "I",
            "Extracts fitness value and genes from a Geospiza individual",
            "Geospiza", "Population")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap Icon => Resources.Individual;

    public override Guid ComponentGuid => new("16A3E3D3-A282-460B-92CC-78C6EF91B8CC");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Individual", "I", "The individual to reinstate", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("Fitness", "F", "The fitness value", GH_ParamAccess.item);
        pManager.AddGenericParameter("Genes", "G", "The genes", GH_ParamAccess.list);
        pManager.AddNumberParameter("Objectives", "Obj",
            "Multi-objective fitness values (obj1, obj2, ...). Empty for single-objective individuals.",
            GH_ParamAccess.list);
        pManager.AddIntegerParameter("Pareto Rank", "PR",
            "Pareto dominance rank. 0 = Pareto-optimal. Empty for single-objective individuals.",
            GH_ParamAccess.item);
        pManager.AddNumberParameter("Crowding Distance", "CD",
            "Crowding distance within the Pareto front. Higher = more isolated = more diverse. Empty for single-objective.",
            GH_ParamAccess.item);
        pManager.AddIntegerParameter("Generation", "Gen",
            "The generation in which this individual was created.",
            GH_ParamAccess.item);
        pManager.AddTextParameter("Id", "Id",
            "Stable unique identifier for this individual. Preserved through copies, snapshots, and JSON. Use this to reliably identify or reinstate a specific individual.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper individualWrapper = null;
        if (!DA.GetData(0, ref individualWrapper)) return;
        var individual = individualWrapper.Value as Individual;
        if (individual == null) return;

        DA.SetData(0, individual.Fitness);
        DA.SetDataList(1, individual.GenePool);
        DA.SetData(5, individual.Generation);
        DA.SetData(6, individual.Id.ToString());

        if (individual.Objectives is { Length: > 0 })
        {
            DA.SetDataList(2, individual.Objectives);
            DA.SetData(3, individual.ParetoRank);
            DA.SetData(4, individual.CrowdingDistance);
        }
    }
}