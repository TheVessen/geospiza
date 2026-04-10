using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Population;

public class GH_GetIndividualById : GH_Component
{
    public GH_GetIndividualById()
        : base("Get Individual by Id", "GetById",
            "Retrieves individuals from the final population snapshot by their stable Id. " +
            "Paste one or more Ids from the AI analysis output.",
            "Geospiza", "Population")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap Icon => Resources.ReinstateIndividuum;

    public override Guid ComponentGuid => new("0A99E2A7-6145-463A-AB60-B76A44D58DDE");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "O", "The EvolutionObserver from a solver", GH_ParamAccess.item);
        pManager.AddTextParameter("Ids", "Id", "One or more individual Ids to look up (full or 8-character prefix)", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Individuals", "I", "Matched individuals, in the order the Ids were provided", GH_ParamAccess.list);
        pManager.AddTextParameter("Missing", "M", "Ids that were not found in the final population", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_ObjectWrapper wrapper = null;
        if (!DA.GetData(0, ref wrapper)) return;

        if (wrapper.Value is not EvolutionObserver obs)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not an EvolutionObserver.");
            return;
        }

        var snapshot = obs.FinalPopulationSnapshot;
        if (snapshot == null || snapshot.Length == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Observer has no final population snapshot. Run the solver first.");
            return;
        }

        var ids = new List<string>();
        if (!DA.GetDataList(1, ids)) return;

        var schema = obs.GeneSchema;
        if (schema == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Observer has no gene schema.");
            return;
        }

        var found = new List<Individual>();
        var missing = new List<string>();

        foreach (var rawId in ids)
        {
            var id = rawId?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                missing.Add(rawId ?? "");
                continue;
            }

            // Match by full Guid string or 8-character prefix
            IndividualSnapshot match;
            if (id.Length == 8)
                match = snapshot.FirstOrDefault(s => s.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase));
            else if (Guid.TryParse(id, out var guid))
                match = snapshot.FirstOrDefault(s => s.Id == guid);
            else
                match = null;

            if (match != null)
                found.Add(match.ToIndividual(schema));
            else
                missing.Add(id);
        }

        DA.SetDataList(0, found);
        if (missing.Count > 0)
            DA.SetDataList(1, missing);
    }
}
