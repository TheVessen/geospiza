using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using Geospiza;
using System.Threading.Tasks;
using Geospiza.Algorythm;
using Geospiza.Core;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Geospiza.Comonents;

public class GH_BasicSolver : GH_Component
{
    /// <summary>
    /// Initializes a new instance of the BasicSolver class.
    /// </summary>
    public GH_BasicSolver()
        : base("SingleObjectiveSolver", "SOS",
            "Solver for single objective optimization problems",
            "Geospiza", "Solvers")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Gene", "GID", "The gene ids from the GeneSelector", GH_ParamAccess.list);
        pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
            GH_ParamAccess.item);
        pManager.AddNumberParameter("Timestamp", "T", "Timestamp from the server to determine if the solver should run",
            GH_ParamAccess.item, 0);
        pManager.AddBooleanParameter("Run", "R", "Run the solver for running locally", GH_ParamAccess.item, false);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Observer", "LP", "The last population", GH_ParamAccess.item);
        pManager.AddNumberParameter("CurrentGeneration", "CG", "The current generation", GH_ParamAccess.item);
    }

    private long _lastTimestamp = 0;
    private EvolutionaryAlgorithmSettings _privateSettings;
    private bool _isRunning = true;
    private  Guid _solutionId = Guid.NewGuid();
    private Guid _lastSolutionId;
    


    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Get inputs
        var geneIds = new List<string>();
        if (!DA.GetDataList(0, geneIds)) return;

        var settings = new EvolutionaryAlgorithmSettings();
        if (!DA.GetData(1, ref settings)) return;
        _privateSettings = settings;

        double timestamp = 0;
        if (!DA.GetData(2, ref timestamp)) return;
        long intTimestamp = Convert.ToInt64(timestamp);
        
        var run = false;
        if (!DA.GetData(3, ref run)) return;
        
        // Check if the solver should run
        if(_lastSolutionId != Guid.Empty && _solutionId != _lastSolutionId)
        {
            return;
        }

        // Set up state manager
        StateManager stateManager = StateManager.Instance;
        stateManager.SetDocument(OnPingDocument());
        stateManager.SetGenes(geneIds);
        stateManager.SetFitnessComponent();
        stateManager.SetThisComponent(this);

        // Check if the solver should run
        bool _run = (intTimestamp != 0 && intTimestamp != _lastTimestamp) || run;

        // Run the solver
        if (_run)
        {
            DA.SetData(0, null);
            _isRunning = true;
            OnPingDocument().ScheduleSolution(100, ScheduleCallback);
            _lastTimestamp = intTimestamp;
        }
    }

    void ScheduleCallback(GH_Document doc)
    {
        _solutionId = Guid.NewGuid();
        Observer.Instance.Reset();

        var evolutionaryAlgorithm = new EvolutionaryAlgorithm(_privateSettings);

        evolutionaryAlgorithm.RunAlgorithm();
        _isRunning = false;
        ExpireSolution(false);
        _lastSolutionId = _solutionId;
    }

    protected override void AfterSolveInstance()
    {
        base.AfterSolveInstance();
        if (_isRunning == false)
        {
            Params.Output[0].ClearData();
            Params.Output[0].AddVolatileData(new GH_Path(0), 0, Observer.Instance);
        }

        Params.Output[1].ClearData();
        Params.Output[1].AddVolatileData(new GH_Path(0), 0, Observer.Instance.CurrentGeneration);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon => Properties.Resources.Solver;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("DC3BBA6C-488E-496C-AE62-5488B065C38F"); }
    }
}