using System;
using System.Collections.Generic;
using System.Drawing;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace GeospizaPlugin.Components.GeneOperations;

/// <summary>
///     Collects gene parameters for evolutionary optimization.
///     Zoom in to add or remove gene slots via the + / - grips.
///     Each input accepts a single slider or gene pool, giving full control over ordering.
/// </summary>
public class GH_GeneCollector : GH_Component, IGH_VariableParameterComponent
{
    //REF "https://discourse.mcneel.com/t/gene-pool-component/59835/8"

    public GH_GeneCollector()
        : base("Gene Collector", "GC",
            "Collects and identifies parameters for evolutionary optimization. Compatible with numeric sliders and Galapagos gene pools. " +
            "Zoom in to add or remove gene slots via the + / - grips.",
            "Geospiza", "GeneOperations")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.GeneSelector;

    public override Guid ComponentGuid => new("DCCF2B6C-6790-4610-821B-F26C2FC938C2");

    // ── IGH_VariableParameterComponent ──────────────────────────────────────

    public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) =>
        side == GH_ParameterSide.Input && Params.Input.Count > 1;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
        return new Param_GenericObject { Access = GH_ParamAccess.list };
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => true;

    public void VariableParameterMaintenance()
    {
        for (var i = 0; i < Params.Input.Count; i++)
        {
            var p = Params.Input[i];

            var isUserRenamedName = !string.IsNullOrEmpty(p.Name) && p.Name != "Data" && !p.Name.StartsWith("Gene ");
            var isUserRenamedNick = !string.IsNullOrEmpty(p.NickName) && p.NickName != "Data" && !System.Text.RegularExpressions.Regex.IsMatch(p.NickName, @"^G\d+$");

            if (!isUserRenamedName) p.Name = $"Gene {i}";
            if (!isUserRenamedNick) p.NickName = $"Gene {i}";

            p.Description = $"Gene parameter {i} (number slider or gene pool).";
            p.Access = GH_ParamAccess.list;
            p.Optional = true;
        }
    }


    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Gene 0", "G0", "Gene parameter (number slider or gene pool).", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Genes", "GID", "The gene ids in the order defined by the inputs.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var geneIds = new List<string>();
        var ignored = new List<string>();

        for (var i = 0; i < Params.Input.Count; i++)
        {
            var sources = Params.Input[i].Sources;
            foreach (var src in sources)
            {
                // Only top-level sliders and gene pools can be driven by the solver; a component
                // output param is not a top-level object, so the StateManager could not find it.
                var srcType = src.GetType().ToString();
                if (srcType == "Grasshopper.Kernel.Special.GH_NumberSlider" ||
                    srcType == "GalapagosComponents.GalapagosGeneListObject")
                    geneIds.Add(src.InstanceGuid.ToString());
                else
                    ignored.Add(src.NickName);
            }
        }

        if (ignored.Count > 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                $"Ignored {ignored.Count} source{(ignored.Count == 1 ? "" : "s")} that " +
                $"{(ignored.Count == 1 ? "is" : "are")} not a number slider or gene pool: {string.Join(", ", ignored)}");

        if (geneIds.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "No gene parameters connected. Connect number sliders or gene pools.");
            return;
        }

        Message = $"{geneIds.Count} gene{(geneIds.Count == 1 ? "" : "s")}";
        DA.SetDataList(0, geneIds);
    }
}
