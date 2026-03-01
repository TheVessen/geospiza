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

namespace GeospizaPlugin.Components.Solvers;

/// <summary>
///     Grasshopper component that runs the NSGA-II multi-objective evolutionary algorithm.
///     Mirrors <see cref="GH_BasicSolver" /> but instantiates <see cref="NsgaIISolver" />.
///     Requires a <c>GH_MultiObjectiveFitness</c> component on the canvas.
/// </summary>
public class GH_NsgaIISolver : GH_Component
{
    private bool _isLocked;
    private bool _isRunning;
    private Guid _lastSolutionId;
    private SolverSettings _privateSettings;
    private Guid _solutionId = Guid.NewGuid();

    public GH_NsgaIISolver()
        : base("NSGA-II Solver", "NSGA2",
            "Runs an NSGA-II multi-objective evolutionary algorithm. " +
            "Connect a Multi-Objective Fitness (MOF) component to supply objectives.",
            "Geospiza", "Solvers")
    {
    }

    private StateManager StateManager { get; set; }
    private EvolutionObserver EvolutionObserver { get; set; }

    protected override Bitmap Icon => Resources.Solver;

    public override Guid ComponentGuid => new("B2C3D4E5-F6A7-8901-BCDE-F12345678901");

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
        updateParam.PersistentData.Append(new GH_Integer(0));
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
            "The EvolutionObserver containing Pareto fronts and per-generation statistics",
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

        var settings = new SolverSettings();
        if (!DA.GetData(1, ref settings)) return;
        _privateSettings = settings;

        var previewLevel = 0;
        if (!DA.GetData(2, ref previewLevel)) return;

        var runButton = false;
        if (!DA.GetData(3, ref runButton)) return;

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
        try
        {
            var cancellationToken = new CancellationTokenSource();

            _solutionId = Guid.NewGuid();
            EvolutionObserver.Reset();

            var solver = new NsgaIISolver(_privateSettings, StateManager, EvolutionObserver);
            solver.RunAlgorithm(cancellationToken.Token);

            OnPingDocument().ScheduleSolution(100, d =>
            {
                _isRunning = false;
                _isLocked = false;
                ExpireSolution(false);
            });

            _lastSolutionId = _solutionId;
        }
        catch (Exception)
        {
            _isLocked = false;
            throw;
        }
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        EvolutionObserver.RemoveInstance(this);
        StateManager.RemoveInstance(this);
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
