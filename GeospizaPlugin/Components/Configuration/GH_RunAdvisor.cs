using System;
using System.Drawing;
using System.Windows.Forms;
using GeospizaCore.Solvers;
using GeospizaPlugin.AI;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Configuration;

/// <summary>
///     Attaches a run advisor to solver settings, letting a decision model (or a text model) choose after
///     each generation whether the solver explores more, holds, or refines.
/// </summary>
public class GH_RunAdvisor : GH_Component
{
    public GH_RunAdvisor()
        : base("Run Advisor", "Advisor",
            "Lets an AI model decide after each generation whether the solver should explore more, keep its " +
            "current rates, or refine, and when to add random designs. The built-in thresholds decide " +
            "whenever the model fails, is too slow or is unsure. Models are set up in AI Settings (right-click).",
            "Geospiza", "Configuration")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap Icon => Resources.Settings;

    public override Guid ComponentGuid => new("0D3F6B7E-6A51-4C59-9E0B-5B8E2C4A7D13");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S", "Settings from a Settings or Multi-Objective Settings component.",
            GH_ParamAccess.item);

        var backend = new Param_Integer
        {
            Name = "Backend",
            NickName = "B",
            Description = "Who makes the decision. Decision model: Jev by TypeSafe (fast, calibrated, needs an " +
                          "API key). Text model: the AI Analysis model answering in JSON (slower). " +
                          "Thresholds: the built-in rules, i.e. the same as not using this component.",
            Access = GH_ParamAccess.item
        };
        backend.AddNamedValue("Thresholds", (int)AdvisorBackend.Thresholds);
        backend.AddNamedValue("Decision model (Jev)", (int)AdvisorBackend.DecisionModel);
        backend.AddNamedValue("Text model (LLM)", (int)AdvisorBackend.TextModel);
        backend.PersistentData.Append(new GH_Integer((int)AdvisorBackend.DecisionModel));
        pManager.AddParameter(backend);

        pManager.AddIntegerParameter("Interval", "I",
            "Ask the model every this many generations; thresholds decide in between. Use 5 or more with " +
            "local text models, which take seconds per answer.",
            GH_ParamAccess.item, 1);
        pManager.AddNumberParameter("Timeout", "T",
            "Seconds to wait for an answer before the thresholds decide that generation.",
            GH_ParamAccess.item, 30);
        pManager.AddNumberParameter("Min Confidence", "C",
            "Decision models only: below this confidence (0-1) the thresholds decide instead.",
            GH_ParamAccess.item, 0.5);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Settings", "S", "The settings with the advisor attached.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var settingWrapper = new GH_ObjectWrapper();
        if (!DA.GetData(0, ref settingWrapper)) return;
        if (settingWrapper.Value is not SolverSettings settings)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "Input is not a SolverSettings object. Connect a Settings or Multi-Objective Settings component.");
            return;
        }

        var backendIndex = (int)AdvisorBackend.DecisionModel;
        DA.GetData(1, ref backendIndex);
        var interval = 1;
        DA.GetData(2, ref interval);
        var timeoutSeconds = 30.0;
        DA.GetData(3, ref timeoutSeconds);
        var minConfidence = 0.5;
        DA.GetData(4, ref minConfidence);

        if (!Enum.IsDefined(typeof(AdvisorBackend), backendIndex))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Backend must be 0 (Thresholds), 1 (Decision model) or 2 (Text model).");
            return;
        }

        if (interval < 1)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Interval must be at least 1; using 1.");
            interval = 1;
        }

        if (timeoutSeconds <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Timeout must be greater than 0 seconds.");
            return;
        }

        var backend = (AdvisorBackend)backendIndex;
        var config = AiConfig.Load();
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        ModelRunAdvisor advisor = null;
        switch (backend)
        {
            case AdvisorBackend.DecisionModel:
                if (config.ResolvedJevApiKey == null)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "No TypeSafe API key yet, so the thresholds will decide. Right-click > AI Settings to add one.");
                advisor = new ModelRunAdvisor(() => AiProviders.CreateJev(AiConfig.Load()),
                    AiProviders.CreateJev(config).Id, interval, timeout, minConfidence);
                break;
            case AdvisorBackend.TextModel:
                advisor = new ModelRunAdvisor(
                    () => new TextModelDecisionAdapter(AiProviders.CreateTextModel(AiConfig.Load())),
                    AiProviders.CreateTextModel(config).Id, interval, timeout, minConfidence);
                break;
        }

        Message = backend switch
        {
            AdvisorBackend.DecisionModel => "Jev",
            AdvisorBackend.TextModel => "LLM",
            _ => "Thresholds"
        };
        DA.SetData(0, settings.WithAdvisor(advisor));
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);
        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "AI Settings...", (_, _) =>
        {
            var dialog = new AiSettingsDialog(showDecisionTab: true);
            dialog.ShowModal();
            if (dialog.Saved) ExpireSolution(true);
        });
    }
}
