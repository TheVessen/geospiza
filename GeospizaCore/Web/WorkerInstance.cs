using System;
using System.Threading;
using Grasshopper.Kernel;


namespace GeospizaPlugin.AsyncComponent
{
    /// <summary>
    /// Base class for asynchronous worker instances
    /// <remarks>
    /// This class is based on the example by speckle systems: <a href="https://github.com/specklesystems/GrasshopperAsyncComponent"> GitHub</a>
    /// </remarks>
    /// </summary>
    public abstract class WorkerInstance
    {
        /// <summary>
        /// The parent Grasshopper component.
        /// </summary>
        protected GH_Component Parent { get; }

        protected WorkerInstance(GH_Component parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Reads input data from the component.
        /// </summary>
        public abstract void GetData(IGH_DataAccess DA);

        /// <summary>
        /// Performs the background work synchronously.
        /// </summary>
        /// <param name="Done">Callback to signal that work is finished.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public abstract void DoWork(Action Done, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the output data once the background work is complete.
        /// </summary>
        public abstract void SetData(IGH_DataAccess DA);

    }
}