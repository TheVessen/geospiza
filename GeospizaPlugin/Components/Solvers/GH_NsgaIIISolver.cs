using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using GeospizaCore.Core;
using GeospizaCore.Solvers;
using GeospizaPlugin.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;

namespace GeospizaPlugin.Components.Solvers;

/// <summary>
///     Grasshopper component that runs the NSGA-III many-objective evolutionary algorithm.
///     Uses structured reference points for diversity preservation — superior to NSGA-II
///     when there are four or more objectives.
///     Requires a <c>GH_MultiObjectiveFitness</c> component on the canvas.
/// </summary>
public class GH_NsgaIIISolver : GH_Component
{
    private bool _isLocked;
    private bool _isRunning;
    private Guid _lastSolutionId;
    private int _privateDivisions = 12;
    private SolverSettings _privateSettings;
    private Guid _solutionId = Guid.NewGuid();

    public GH_NsgaIIISolver()
        : base("NSGA-III Solver", "NSGA3",
            "Runs an NSGA-III many-objective evolutionary algorithm. " +
            "Uses reference-point-based diversity preservation, which outperforms NSGA-II on 4+ objectives. " +
            "Connect a Multi-Objective Fitness (MOF) component to supply objectives.",
            "Geospiza", "Solvers")
    {
    }

    private StateManager StateManager { get; set; }
    private EvolutionObserver EvolutionObserver { get; set; }

    protected override Bitmap Icon => Resources.Solver;

    public override Guid ComponentGuid => new("C3D4E5F6-A7B8-9012-CDEF-012345678902");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter(
            "Genes",
            "GID",
            "The gene IDs from the GeneSelector",
            GH_ParamAccess.list
        );

        pManager.AddGenericParameter(
            "Settings",
            "S",
            "The settings for the evolutionary algorithm",
            GH_ParamAccess.item
        );

        var updateParam = new Param_Integer();
        updateParam.AddNamedValue("All", 0);
        updateParam.AddNamedValue("Every Generation", 1);
        updateParam.AddNamedValue("If Better", 2);
        updateParam.AddNamedValue("None", 3);
        updateParam.PersistentData.Append(new GH_Integer(3));
        pManager.AddParameter(
            updateParam,
            "PreviewLevel",
            "PL",
            "How often the preview should update:\n" +
            "• 0: Every solution\n" +
            "• 1: Every generation\n" +
            "• 2: Only if a better solution is found\n" +
            "• 3: None (no preview)\n\n" +
            "More frequent preview updates take longer.",
            GH_ParamAccess.item
        );

        var divisionsParam = new Param_Integer();
        divisionsParam.AddNamedValue("Few (4)", 4);
        divisionsParam.AddNamedValue("Moderate (8)", 8);
        divisionsParam.AddNamedValue("Default (12)", 12);
        divisionsParam.AddNamedValue("Dense (20)", 20);
        divisionsParam.PersistentData.Append(new GH_Integer(12));
        pManager.AddParameter(
            divisionsParam,
            "Divisions",
            "D",
            "Reference point density on the objective hyperplane.\n" +
            "Higher values give more uniform diversity coverage but increase computation.\n" +
            "• 4  → few reference points (fast, low objective count)\n" +
            "• 12 → default (recommended for 3–5 objectives)\n" +
            "• 20 → dense (for 2–3 objectives with large populations)",
            GH_ParamAccess.item
        );

        pManager.AddBooleanParameter(
            "Run",
            "R",
            "Set to true to run the solver locally",
            GH_ParamAccess.item,
            false
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "LP",
            "The EvolutionObserver containing Pareto fronts, per-generation statistics, and hypervolume",
            GH_ParamAccess.item);
        pManager.AddGenericParameter("State Manager", "SM", "The StateManager handling gene states",
            GH_ParamAccess.item);
        pManager.AddNumberParameter("Current Generation", "CG", "The current generation index",
            GH_ParamAccess.item);
        pManager.AddBooleanParameter("Is Running", "IR", "Indicates whether the solver is running",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (_isLocked)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Solver is currently running. Please wait.");
            return;
        }

        var geneIds = new List<string>();
        if (!DA.GetDataList(0, geneIds)) return;
        if (geneIds.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "No gene IDs provided. Connect a Gene Collector component.");
            return;
        }

        var settings = new SolverSettings();
        if (!DA.GetData(1, ref settings)) return;
        _privateSettings = settings;

        var previewLevel = 0;
        if (!DA.GetData(2, ref previewLevel)) return;

        var divisions = 12;
        if (!DA.GetData(3, ref divisions)) return;
        _privateDivisions = divisions;

        var runButton = false;
        if (!DA.GetData(4, ref runButton)) return;

        // Validate only when user attempts to run
        if (runButton)
        {
            if (!settings.IsMultiObjective)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Single-objective Settings are not compatible with NSGA-III. " +
                    "Use the Multi-Objective Settings component instead.");
                return;
            }

            // Check for Multi-Objective Fitness — force a solve first so the MOF component
            // has a chance to populate the singleton (avoids stale empty state on file open).
            OnPingDocument().NewSolution(false);
            if (GeospizaCore.Core.Fitness.Instance.GetObjectives().Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No multi-objective fitness found. " +
                    "Connect a Multi-Objective Fitness (MOF) component with at least one objective. " +
                    "The single-objective Fitness component does not work with NSGA-III.");
                return;
            }
        }

        if (_lastSolutionId != Guid.Empty && _solutionId != _lastSolutionId)
            return;

        StateManager ??= StateManager.GetInstance(this, OnPingDocument());
        EvolutionObserver ??= EvolutionObserver.GetInstance(this);
        StateManager.SetGenes(geneIds);
        StateManager.PreviewLevel = previewLevel;

        if (runButton)
        {
            DA.SetData(0, null);
            _isRunning = true;
            _isLocked = true;
            OnPingDocument().ScheduleSolution(100, ScheduleCallback);
        }
    }

    private void ScheduleCallback(GH_Document doc)
    {
        var maxGenerations = _privateSettings.MaxGenerations;

        void OnGenerationCompleted(object sender, EvolutionObserver.GenerationCompletedEventArgs e)
        {
            Message = $"Gen {e.GenerationIndex}/{maxGenerations}";
            OnDisplayExpired(true);
            RhinoApp.Wait();
        }

        try
        {
            StateManager.RunCts = new CancellationTokenSource();
            _solutionId = Guid.NewGuid();

            EvolutionObserver.Reset();
            EvolutionObserver.GenerationCompleted += OnGenerationCompleted;

            Message = "Running...";
            OnDisplayExpired(true);
            RhinoApp.Wait();

            var solver = new NsgaIIISolver(_privateSettings, StateManager, EvolutionObserver, _privateDivisions);
            solver.RunAlgorithm(StateManager.RunCts.Token);

            Message = "Done";
            _lastSolutionId = _solutionId;
        }
        catch (Exception)
        {
            Message = "Error";
            throw;
        }
        finally
        {
            EvolutionObserver.GenerationCompleted -= OnGenerationCompleted;
            StateManager.RunCts?.Dispose();
            StateManager.RunCts = null;
            _isRunning = false;
            _isLocked = false;
            EvolutionObserver.NotifyRunCompleted();
        }
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        EvolutionObserver.RemoveInstance(this);
        EvolutionObserver = null;
        StateManager.RemoveInstance(this);
        StateManager = null;
        base.RemovedFromDocument(document);
    }

    protected override void AfterSolveInstance()
    {
        base.AfterSolveInstance();

        if (!_isRunning)
        {
            Params.Output[0].ClearData();
            Params.Output[0].AddVolatileData(new GH_Path(0), 0, EvolutionObserver);

            Params.Output[1].ClearData();
            Params.Output[1].AddVolatileData(new GH_Path(0), 0, StateManager);
        }

        Params.Output[2].ClearData();
        Params.Output[2].AddVolatileData(new GH_Path(0), 0, EvolutionObserver?.CurrentGenerationIndex ?? 0);

        Params.Output[3].ClearData();
        Params.Output[3].AddVolatileData(new GH_Path(0), 0, _isRunning);
    }
}