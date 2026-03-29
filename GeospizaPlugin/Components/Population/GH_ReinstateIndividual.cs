using System;
using System.Drawing;
using System.Linq;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Population;

public class GH_ReinstateIndividual : GH_Component
{
    private Individual individual;
    private StateManager stateManager;

    public GH_ReinstateIndividual()
        : base("Reinstate Individual", "Reinstate Individual",
            "Reactivates a previously archived individual in the current population",
            "Geospiza", "Population")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap Icon => Resources.ReinstateIndividuum;

    public override Guid ComponentGuid => new("6692F05D-9C4F-4B99-B4C5-CF1D91C7B639");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("State Manager", "SM", "The state managers", GH_ParamAccess.item);
        pManager.AddGenericParameter("Individual", "I", "The individual to reinstate", GH_ParamAccess.tree);
        pManager.AddBooleanParameter("Reinstate", "R", "Reinstate the individual", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper stateManagerWrapper = null;
        if (!DA.GetData(0, ref stateManagerWrapper)) return;

        stateManager = stateManagerWrapper.Value as StateManager;
        if (stateManager == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "Input is not a StateManager. Connect the State Manager output from a solver component.");
            return;
        }

        var individualWrapper = new GH_Structure<IGH_Goo>();
        if (!DA.GetDataTree(1, out individualWrapper)) return;

        var data = individualWrapper.AllData(true).ToList();
        if (data.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No individual provided.");
            return;
        }

        if (data.Count != 1)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a single individual.");
            return;
        }

        individual = data[0].ScriptVariable() as Individual;
        if (individual == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "Input is not an Individual. Connect an Individual output from a results component.");
            return;
        }

        var reinstate = false;
        if (!DA.GetData(2, ref reinstate)) return;

        if (reinstate)
        {
            Message = "Reinstated";
            OnPingDocument().ScheduleSolution(10, ScheduleCallback);
        }
        else
        {
            Message = "Ready";
        }
    }

    private void ScheduleCallback(GH_Document doc)
    {
        OnPingDocument().NewSolution(false);
        individual.Reinstate(stateManager);
        ExpirePreview(false);
        ExpireSolution(false);
    }
}