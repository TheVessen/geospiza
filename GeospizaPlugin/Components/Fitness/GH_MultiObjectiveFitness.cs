using System;
using System.Drawing;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace GeospizaPlugin.Components.Fitness;

/// <summary>
///     Multi-objective fitness component with zoom-sensitive variable inputs (ZUI).
///     Zoom in on the component to reveal + / - grips for adding or removing fitness objectives.
///     Starts with two inputs ("Fitness 0", "Fitness 1"); requires at least two.
///     Used together with <c>GH_NsgaIISolver</c> for NSGA-II multi-objective optimization.
/// </summary>
public class GH_MultiObjectiveFitness : GH_Component, IGH_VariableParameterComponent
{
    public GH_MultiObjectiveFitness()
        : base("Multi-Objective Fitness", "MOF",
            "Assigns multiple fitness objectives for multi-objective evolutionary optimization (NSGA-II). " +
            "Zoom in to add or remove objectives via the + / - grips.",
            "Geospiza", "Fitness")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap Icon => Resources.Fitness;

    public override Guid ComponentGuid => new("0C7C9C4C-13BF-491F-BDA5-871C7292D842");

    // ── IGH_VariableParameterComponent ──────────────────────────────────────

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
        return side == GH_ParameterSide.Input;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
        return side == GH_ParameterSide.Input && Params.Input.Count > 2;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
        return new Param_Number { Access = GH_ParamAccess.item };
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
        return true;
    }

    public void VariableParameterMaintenance()
    {
        for (var i = 0; i < Params.Input.Count; i++)
        {
            // Only auto-name if the parameter still has a default name — preserve user renames.
            if (string.IsNullOrEmpty(Params.Input[i].Name) || Params.Input[i].Name.StartsWith("Fitness "))
            {
                Params.Input[i].Name = $"Fitness {i}";
                Params.Input[i].NickName = $"F{i}";
            }

            Params.Input[i].Description = $"Fitness objective {i} for multi-objective optimization.";
            Params.Input[i].Optional = i >= 1;
        }
    }

    // ── GH_Component overrides ───────────────────────────────────────────────

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Fitness 0", "F0", "First fitness objective.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Fitness 1", "F1", "Second fitness objective.", GH_ParamAccess.item, 0);
        Params.Input[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        // No outputs — values are stored in the Fitness singleton.
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var count = Params.Input.Count;
        var objectives = new double[count];
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            double val = 0;
            DA.GetData(i, ref val);
            objectives[i] = val;
            names[i] = Params.Input[i].Name;
        }

        GeospizaCore.Core.Fitness.Instance.SetObjectives(objectives);
        GeospizaCore.Core.Fitness.Instance.SetObjectiveNames(names);
    }
}