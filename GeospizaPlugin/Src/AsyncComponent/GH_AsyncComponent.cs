// Based on https://github.com/specklesystems/GrasshopperAsyncComponent
// Licensed under MIT — Copyright (c) Speckle Systems

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Grasshopper.Kernel;
using Rhino;
using Timer = System.Timers.Timer;

namespace GeospizaPlugin.AsyncComponent;

internal sealed class Worker<T> : IDisposable
    where T : GH_Component
{
    public Worker(WorkerInstance<T> instance, Task task, CancellationTokenSource cancellationSource)
    {
        Instance = instance;
        Task = task;
        CancellationSource = cancellationSource;
    }

    public WorkerInstance<T> Instance { get; }

    public Task Task { get; }

    public CancellationTokenSource CancellationSource { get; }

    public void Dispose()
    {
        if (Task.IsCompleted) Task.Dispose();
        CancellationSource.Dispose();
    }

    public void Cancel()
    {
        CancellationSource.Cancel();
    }
}

/// <summary>
///     Inherit your component from this class to make all the async goodness available.
/// </summary>
public abstract class GH_AsyncComponent<T> : GH_Component, IDisposable
    where T : GH_Component
{
    private readonly Timer _displayProgressTimer;
    private readonly Action<string, double> _reportProgress;

    private readonly List<Worker<T>> _workers;

    private bool _isDisposed;

    /// <summary>
    ///     functionally, a boolean, 1 or 0;
    ///     it will be set to 1 once all workers are ready for SetData to be called on them, then set back to 0.
    /// </summary>
    private volatile int _setData;

    /// <summary>
    ///     a counter, used to count up the number of workers that have completed,
    ///     until _setData is set true, when it starts to count down the workers as their data is set.
    /// </summary>
    private volatile int _state;

    protected GH_AsyncComponent(string name, string nickname, string description, string category, string subCategory)
        : base(name, nickname, description, category, subCategory)
    {
        _workers = new List<Worker<T>>();

        ProgressReports = new ConcurrentDictionary<string, double>();

        _displayProgressTimer = new Timer(333) { AutoReset = false };
        _displayProgressTimer.Elapsed += DisplayProgress;

        _reportProgress = (id, value) =>
        {
            ProgressReports[id] = value;
            if (!_displayProgressTimer.Enabled) _displayProgressTimer.Start();
        };
    }

    public ConcurrentDictionary<string, double> ProgressReports { get; }

    public IEnumerable<CancellationTokenSource> CancellationTokenSources => _workers.Select(x => x.CancellationSource);
    public IEnumerable<WorkerInstance<T>> Workers => _workers.Select(x => x.Instance);

    public int WorkerCount => _workers.Count;

    /// <summary>
    ///     Set this property inside the constructor of your derived component.
    /// </summary>
    public WorkerInstance<T> BaseWorker { get; set; }

    /// <summary>
    ///     Optional: if you have opinions on how the default system task scheduler should treat your workers, set it here.
    /// </summary>
    public TaskCreationOptions TaskCreationOptions { get; set; } = TaskCreationOptions.None;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Done()
    {
        Interlocked.Increment(ref _state);
        if (_state == _workers.Count && _setData == 0)
        {
            Interlocked.Exchange(ref _setData, 1);

            // We need to reverse the workers list to set the outputs in the same order as the inputs.
            _workers.Reverse();

            RhinoApp.InvokeOnUiThread(
                (Action)
                delegate { ExpireSolution(true); }
            );
        }
    }

    public virtual void DisplayProgress(object sender, ElapsedEventArgs e)
    {
        if (_workers.Count == 0 || ProgressReports.Values.Count == 0) return;

        if (_workers.Count == 1)
        {
            Message = ProgressReports.Values.Last().ToString("0.00%");
        }
        else
        {
            double total = 0;
            foreach (var kvp in ProgressReports) total += kvp.Value;

            Message = (total / _workers.Count).ToString("0.00%");
        }

        RhinoApp.InvokeOnUiThread(
            (Action)
            delegate { OnDisplayExpired(true); }
        );
    }

    protected override void BeforeSolveInstance()
    {
        if (_state != 0 && _setData == 1) return;

        Debug.WriteLine("Killing");

        foreach (var currentWorker in _workers) currentWorker.Cancel();

        ResetState();
    }

    protected override void AfterSolveInstance()
    {
        Debug.WriteLine("After solve instance was called " + _state + " ? " + _workers.Count);
        // We need to start all the tasks as close as possible to each other.
        if (_state == 0 && _workers.Count > 0 && _setData == 0)
        {
            Debug.WriteLine("After solve INVOCATION");
            foreach (var worker in _workers) worker.Task.Start();
        }
    }

    protected override void ExpireDownStreamObjects()
    {
        // Prevents the flash of null data until the new solution is ready
        if (_setData == 1) base.ExpireDownStreamObjects();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (_state == 0)
        {
            if (BaseWorker == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Worker class not provided.");
                return;
            }

            // Add cancellation source to our bag
            var tokenSource = new CancellationTokenSource();

            var currentWorker = BaseWorker.Duplicate($"Worker-{DA.Iteration}", tokenSource.Token);

            // Let the worker collect data.
            currentWorker.GetData(DA, Params);

            var currentRun = new Task<Task>(
                async () => { await currentWorker.DoWork(_reportProgress, Done).ConfigureAwait(true); },
                tokenSource.Token,
                TaskCreationOptions
            );

            // Add the worker to our list
            _workers.Add(new Worker<T>(currentWorker, currentRun, tokenSource));

            return;
        }

        if (_setData == 0) return;

        if (_workers.Count > 0)
        {
            Interlocked.Decrement(ref _state);
            _workers[_state].Instance.SetData(DA);
        }

        if (_state != 0) return;

        foreach (var worker in _workers) worker?.Dispose();

        ResetState();

        Message = "Done";
        OnDisplayExpired(true);
    }

    private void ResetState()
    {
        _workers.Clear();
        ProgressReports.Clear();

        Interlocked.Exchange(ref _state, 0);
        Interlocked.Exchange(ref _setData, 0);
    }

    public void RequestCancellation()
    {
        foreach (var worker in _workers) worker.Cancel();

        ResetState();
        Message = "Cancelled";
        OnDisplayExpired(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                foreach (var worker in _workers) worker?.Dispose();
                _displayProgressTimer?.Dispose();
            }
        }
    }
}