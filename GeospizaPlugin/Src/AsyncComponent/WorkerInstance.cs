// Based on https://github.com/specklesystems/GrasshopperAsyncComponent
// Licensed under MIT — Copyright (c) Speckle Systems

using System;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace GrasshopperAsyncComponent;

/// <summary>
/// A class that holds the actual compute logic and encapsulates the state it needs.
/// Every <see cref="GH_AsyncComponent{T}"/> needs to have one.
/// </summary>
public abstract class WorkerInstance<T>(T parent, string id, CancellationToken cancellationToken)
    where T : GH_Component
{
    /// <summary>
    /// The parent component. Useful for passing state back to the host component.
    /// </summary>
    public T Parent { get; set; } = parent;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public string Id { get; set; } = id;

    /// <summary>
    /// This is a "factory" method. It should return a fresh instance of this class,
    /// but with all the necessary state that you might have passed on directly from your component.
    /// </summary>
    public abstract WorkerInstance<T> Duplicate(string id, CancellationToken cancellationToken);

    /// <summary>
    /// This method is where the actual calculation/computation/heavy lifting should be done.
    /// Make sure you always check as frequently as you can if <see cref="CancellationToken"/> is cancelled.
    /// </summary>
    public abstract Task DoWork(Action<string, double> reportProgress, Action done);

    /// <summary>
    /// Write your data setting logic here. It will be invoked by the parent <see cref="GH_AsyncComponent{T}"/>
    /// after you've called Done in the <see cref="DoWork"/> function.
    /// </summary>
    public abstract void SetData(IGH_DataAccess da);

    /// <summary>
    /// Write your data collection logic here. It will be invoked by the parent <see cref="GH_AsyncComponent{T}"/>.
    /// </summary>
    public abstract void GetData(IGH_DataAccess da, GH_ComponentParamServer parameters);
}
