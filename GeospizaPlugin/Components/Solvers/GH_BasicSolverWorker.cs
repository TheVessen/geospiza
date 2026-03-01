using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeospizaCore.Core;
using GeospizaCore.Solvers;
using GrasshopperAsyncComponent;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.Solvers;

public class GH_BasicSolverWorker : WorkerInstance<GH_BasicSolver>
{
    private List<string> _geneIds;
    private SolverSettings _settings;
    private int _previewLevel;
    private bool _run;

    private StateManager _stateManager;
    private EvolutionObserver _evolutionObserver;

    public GH_BasicSolverWorker(GH_BasicSolver parent, string id, CancellationToken cancellationToken)
        : base(parent, id, cancellationToken)
    {
    }

    public override WorkerInstance<GH_BasicSolver> Duplicate(string id, CancellationToken cancellationToken)
    {
        return new GH_BasicSolverWorker(Parent, id, cancellationToken);
    }

    public override void GetData(IGH_DataAccess da, GH_ComponentParamServer parameters)
    {
        _geneIds = new List<string>();
        if (!da.GetDataList(0, _geneIds)) return;

        _settings = new SolverSettings();
        if (!da.GetData(1, ref _settings)) return;

        _previewLevel = 0;
        da.GetData(2, ref _previewLevel);

        _run = false;
        da.GetData(3, ref _run);

        _stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
        _evolutionObserver = EvolutionObserver.GetInstance(Parent);
        _stateManager.SetGenes(_geneIds);
        _stateManager.PreviewLevel = _previewLevel;
    }

    public override Task DoWork(Action<string, double> reportProgress, Action done)
    {
        if (!_run || _settings == null)
        {
            done();
            return Task.CompletedTask;
        }

        var maxGenerations = _settings.MaxGenerations;

        void OnGenerationCompleted(object sender, EvolutionObserver.GenerationCompletedEventArgs e)
        {
            if (CancellationToken.IsCancellationRequested) return;
            var progress = (double)e.GenerationIndex / maxGenerations;
            reportProgress(Id, progress);
        }

        try
        {
            _evolutionObserver.Reset();
            _evolutionObserver.GenerationCompleted += OnGenerationCompleted;

            var solver = new BaseSolver(_settings, _stateManager, _evolutionObserver);
            solver.RunAlgorithm(CancellationToken);
        }
        finally
        {
            _evolutionObserver.GenerationCompleted -= OnGenerationCompleted;
            done();
        }

        return Task.CompletedTask;
    }

    public override void SetData(IGH_DataAccess da)
    {
        da.SetData(0, _evolutionObserver);
        da.SetData(1, _stateManager);
        da.SetData(2, _evolutionObserver.CurrentGenerationIndex);
        da.SetData(3, false);
    }
}
