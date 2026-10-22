using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeospizaCore.Core;
using GeospizaPlugin.AI;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.Components.Analysis;

public class GH_AiAnalysis : GH_Component
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

    private bool _isRunning;
    private bool _runInputWasTrue;
    private CancellationTokenSource _analysisCts;
    private bool _cancelRequested;
    private string _analysisResult = string.Empty;
    private string _statusMessage = "Ready";
    private string _lastPrompt = string.Empty;

    public GH_AiAnalysis()
        : base("AI Analysis", "AI",
            "Analyses an evolutionary run with the text model chosen in AI Settings (right-click): " +
            "a local Ollama model by default, or any OpenAI-compatible endpoint.",
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
            "Canvas Context", "C",
            "When true, scans the Grasshopper canvas and injects a short LLM-generated summary "
            + "of what the graph computes as extra context. Cached per-canvas-topology so slider "
            + "tweaks don't re-summarise; the first prompt against a new canvas costs one extra LLM call.",
            GH_ParamAccess.item,
            false);

        pManager.AddBooleanParameter(
            "Run", "R",
            "Starts an analysis when it turns true. Right-click the component to cancel a running one.",
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
        // Start only when Run turns true: the analysis' closing ExpireSolution re-solves with a
        // toggle still true, which would otherwise query the model again immediately.
        var run = false;
        DA.GetData(4, ref run);
        var startRun = run && !_runInputWasTrue;
        _runInputWasTrue = run;

        if (_isRunning)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Analysis is in progress. Right-click to cancel.");
            return;
        }

        if (!startRun) return;

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
        var mode = (AnalysisMode)Math.Max(0, Math.Min(2, modeInt));

        var includeCanvasContext = false;
        DA.GetData(3, ref includeCanvasContext);

        if (mode != AnalysisMode.Explanation && obs.CurrentGenerationIndex == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Observer has no data yet. Run a solver first.");
            return;
        }

        // Read the canvas here on the UI thread: walking doc.Objects from the background task
        // races user edits ("Collection was modified").
        string canvasFingerprint = null;
        string canvasDescription = null;
        var doc = includeCanvasContext ? OnPingDocument() : null;
        if (doc != null)
        {
            try
            {
                canvasFingerprint = CanvasFingerprint.Compute(doc);
                canvasDescription = CanvasExtractor.Extract(doc);
            }
            catch (Exception ex)
            {
                canvasFingerprint = null;
                canvasDescription = null;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Canvas context skipped: {ex.Message}");
            }
        }

        _isRunning = true;
        _cancelRequested = false;
        _statusMessage = "Analysing...";
        Message = "Analysing...";
        // Provisional debug prompt without canvas context; replaced once the background task builds
        // the real one (which may first summarise the canvas).
        _lastPrompt = FormatPrompt(PromptBuilder.Build(obs, prompt, mode));
        OnDisplayExpired(true);

        var cts = new CancellationTokenSource(RequestTimeout);
        _analysisCts = cts;
        Task.Run(() => AnalyseAsync(obs, prompt, mode, canvasFingerprint, canvasDescription, cts));
    }

    private async Task AnalyseAsync(EvolutionObserver obs, string prompt, AnalysisMode mode,
        string canvasFingerprint, string canvasDescription, CancellationTokenSource cts)
    {
        var token = cts.Token;
        try
        {
            var model = AiProviders.CreateTextModel(AiConfig.Load());
            var status = await model.CheckAsync(token).ConfigureAwait(false);
            if (status.State == ProviderState.NeedsSetup)
            {
                SetStatus("Waiting for AI Settings...", "Setup...");
                if (!await AiSettingsDialog.ShowOnUiThreadAsync(status.Message).ConfigureAwait(false))
                {
                    SetStatus($"AI setup was cancelled. {status.Message}", "Setup needed");
                    return;
                }

                model = AiProviders.CreateTextModel(AiConfig.Load());
                status = await model.CheckAsync(token).ConfigureAwait(false);
            }

            if (!status.IsReady)
            {
                SetStatus(status.Message, "Unavailable");
                return;
            }

            // Optional canvas summarization step. Cached per topology + model, so this only
            // triggers an extra LLM call the first time a model sees a given graph layout.
            string canvasSummary = null;
            if (canvasFingerprint != null && canvasDescription != null)
            {
                var alreadyCached = CanvasSummarizer.IsCached(canvasFingerprint, model.Id);
                SetStatus(alreadyCached ? "Loading cached canvas summary..." : $"Summarising canvas with {model.Id}...",
                    alreadyCached ? "Canvas..." : "Canvas (new)...");
                canvasSummary = await CanvasSummarizer.GetSummaryAsync(canvasFingerprint, canvasDescription, model, token)
                    .ConfigureAwait(false);
            }

            SetStatus($"Querying {model.Id}...", "Analysing...");
            var userPrompt = PromptBuilder.Build(obs, prompt, mode, canvasSummary);
            _lastPrompt = FormatPrompt(userPrompt);

            _analysisResult = await model.GenerateAsync(PromptBuilder.SystemPrompt, userPrompt, false, token)
                .ConfigureAwait(false);
            SetStatus("Done", "Done");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            SetStatus(_cancelRequested
                    ? "Cancelled."
                    : $"Request timed out ({RequestTimeout.TotalMinutes:0} min limit).",
                _cancelRequested ? "Cancelled" : "Timeout");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.GetBaseException().Message}", "Error");
        }
        finally
        {
            _isRunning = false;
            if (ReferenceEquals(_analysisCts, cts)) _analysisCts = null;
            cts.Dispose();
            RhinoApp.InvokeOnUiThread(() => ExpireSolution(true));
        }
    }

    private void SetStatus(string status, string message)
    {
        _statusMessage = status;
        RhinoApp.InvokeOnUiThread(() =>
        {
            Message = message;
            OnDisplayExpired(true);
        });
    }

    private static string FormatPrompt(string userPrompt)
        => $"[system]\n{PromptBuilder.SystemPrompt}\n[user]\n{userPrompt}";

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

        Menu_AppendItem(menu, "Cancel Analysis", OnCancelClicked, _isRunning);

        var model = AiProviders.CreateTextModel(AiConfig.Load());
        var item = Menu_AppendItem(menu, $"AI Settings (text model: {model.Id})...", OnSettingsClicked, !_isRunning);
        item.ToolTipText = "Choose Ollama or an OpenAI-compatible endpoint, download models, and set up decision models.";
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        _cancelRequested = true;
        try
        {
            _analysisCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The analysis finished between the menu opening and the click.
        }
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        new AiSettingsDialog().ShowModal();
    }
}
