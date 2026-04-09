using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeospizaCore.Core;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.Components.Analysis;

public class GH_AiAnalysis : GH_Component
{
    private bool _isRunning;
    private string _analysisResult = string.Empty;
    private string _statusMessage = "Ready";
    private string _lastPrompt = string.Empty;

    public GH_AiAnalysis()
        : base("AI Analysis", "AI",
            "Analyses an evolutionary run using a local Gemma model via Ollama",
            "Geospiza", "Analysis")
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap Icon => Resources.Observer;

    public override Guid ComponentGuid => new("75AC0982-883A-4958-9406-4E6D01701079");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter(
            "Observer", "O",
            "The completed EvolutionObserver from a solver",
            GH_ParamAccess.item);

        pManager.AddTextParameter(
            "Prompt", "P",
            "Your question or analysis request",
            GH_ParamAccess.item,
            "");

        var modeParam = new Grasshopper.Kernel.Parameters.Param_Integer();
        modeParam.Name = "Mode";
        modeParam.NickName = "M";
        modeParam.Description = "Analysis mode";
        modeParam.Access = GH_ParamAccess.item;
        modeParam.AddNamedValue("Last Generation", 0);
        modeParam.AddNamedValue("Settings Feedback", 1);
        modeParam.AddNamedValue("Explanation", 2);
        modeParam.PersistentData.Append(new GH_Integer(1));
        pManager.AddParameter(modeParam);

        pManager.AddBooleanParameter(
            "Run", "R",
            "Set to true to trigger analysis",
            GH_ParamAccess.item,
            false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Analysis", "A", "The AI analysis response", GH_ParamAccess.item);
        pManager.AddTextParameter("Status", "S", "Current status message", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Is Ready", "R", "True when not busy", GH_ParamAccess.item);
        pManager.AddTextParameter("Prompt", "P", "The full prompt sent to the model (debug)", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (_isRunning)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Analysis is in progress...");
            return;
        }

        GH_ObjectWrapper observerWrapper = null;
        if (!DA.GetData(0, ref observerWrapper)) return;

        if (observerWrapper?.Value is not EvolutionObserver obs)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not an EvolutionObserver.");
            return;
        }

        var prompt = string.Empty;
        DA.GetData(1, ref prompt);

        var modeInt = 1;
        DA.GetData(2, ref modeInt);
        var mode = (AI.AnalysisMode)Math.Max(0, Math.Min(2, modeInt));

        var run = false;
        DA.GetData(3, ref run);

        if (!run) return;

        if (mode != AI.AnalysisMode.Explanation && obs.CurrentGenerationIndex == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Observer has no data yet. Run a solver first.");
            return;
        }

        _isRunning = true;
        _statusMessage = "Analysing...";
        Message = "Analysing...";
        _lastPrompt = AI.PromptBuilder.Build(obs, prompt, mode);
        OnDisplayExpired(true);

        var capturedObs = obs;
        var capturedPrompt = prompt;
        var capturedMode = mode;

        Task.Run(async () =>
        {
            try
            {
                // Check Ollama availability
                if (!await AI.OllamaService.IsAvailableAsync())
                {
                    _analysisResult = string.Empty;
                    _statusMessage = "Ollama is not running. Install and start Ollama from https://ollama.com";
                    return;
                }

                // Load or create config
                var config = AI.AiConfig.Load();

                if (!config.SetupComplete || !await AI.OllamaService.HasModelAsync(config.SelectedModel))
                {
                    // Show setup dialog on the UI thread
                    string? chosen = null;
                    RhinoApp.InvokeOnUiThread(() =>
                    {
                        var dialog = new AI.ModelSetupDialog();
                        dialog.ShowModal();
                        chosen = dialog.SelectedModel;
                    });

                    config = AI.AiConfig.Load();
                    if (!config.SetupComplete)
                    {
                        _statusMessage = "Model setup was cancelled. Select a model to continue.";
                        return;
                    }
                }

                _statusMessage = $"Querying {config.SelectedModel}...";

                var fullPrompt = AI.PromptBuilder.Build(capturedObs, capturedPrompt, capturedMode);

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var result = await AI.OllamaService.GenerateAsync(config.SelectedModel, fullPrompt, cts.Token);

                _analysisResult = result;
                _statusMessage = "Done";
                RhinoApp.InvokeOnUiThread(() => Message = "Done");
            }
            catch (OperationCanceledException)
            {
                _statusMessage = "Request timed out (5 min limit).";
                RhinoApp.InvokeOnUiThread(() => Message = "Timeout");
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error: {ex.Message}";
                RhinoApp.InvokeOnUiThread(() => Message = "Error");
            }
            finally
            {
                _isRunning = false;
                RhinoApp.InvokeOnUiThread(() => ExpireSolution(true));
            }
        });
    }

    protected override void AfterSolveInstance()
    {
        base.AfterSolveInstance();

        Params.Output[0].ClearData();
        Params.Output[0].AddVolatileData(new GH_Path(0), 0, new GH_String(_analysisResult));

        Params.Output[1].ClearData();
        Params.Output[1].AddVolatileData(new GH_Path(0), 0, new GH_String(_statusMessage));

        Params.Output[2].ClearData();
        Params.Output[2].AddVolatileData(new GH_Path(0), 0, new GH_Boolean(!_isRunning));

        Params.Output[3].ClearData();
        Params.Output[3].AddVolatileData(new GH_Path(0), 0, new GH_String(_lastPrompt));
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);
        Menu_AppendSeparator(menu);

        var config = AI.AiConfig.Load();
        var item = Menu_AppendItem(menu, $"Change AI Model (current: {config.SelectedModel})...", OnChangeModelClicked);
        item.ToolTipText = "Opens the model setup dialog to download or switch to a different Gemma model.";
    }

    private void OnChangeModelClicked(object sender, EventArgs e)
    {
        if (_isRunning)
        {
            Rhino.UI.Dialogs.ShowMessage("Analysis is currently in progress. Wait for it to finish before changing the model.", "Geospiza AI");
            return;
        }

        var dialog = new AI.ModelSetupDialog();
        dialog.ShowModal();
    }
}
