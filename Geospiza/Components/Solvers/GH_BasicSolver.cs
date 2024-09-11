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
using Grasshopper.Kernel.Parameters;
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
        _stateManager = StateManager.GetInstance(this);
        _evolutionObserver = EvolutionObserver.GetInstance(this);;
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Genes", "GID", "The gene ids from the GeneSelector", GH_ParamAccess.list);
        pManager.AddGenericParameter("Settings", "S", "The settings for the evolutionary algorithm",
            GH_ParamAccess.item);
        
        //Preview level
        Param_Integer updateParam = new Param_Integer();
        updateParam.AddNamedValue("All", 0);
        updateParam.AddNamedValue("EveryGeneration", 1);
        updateParam.AddNamedValue("IfBetter", 2);
        updateParam.AddNamedValue("None", 3);
        updateParam.PersistentData.Append(new GH_Integer(0));
        pManager.AddParameter(updateParam, "PreviewLevel", "PL", "Set how often the preview should update." +
                                                                 "" +
                                                                 ": 0 if every solution should be shown" +
                                                                 ": 1 if only on every generation the preview should update" +
                                                                 ": 2 if only better solutions should be shown" +
                                                                 ": 4 for no preview" +
                                                                 "" +
                                                                 "The more the preview updated the longer it takes ", GH_ParamAccess.item);
        
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
        pManager.AddGenericParameter("StateManager", "SM", "The state managers", GH_ParamAccess.item);
        pManager.AddNumberParameter("CurrentGeneration", "CG", "The current generation", GH_ParamAccess.item);
        pManager.AddBooleanParameter("IsRunning", "IR", "Is the solver running", GH_ParamAccess.item);
    }
    
    private readonly StateManager _stateManager;
    private readonly EvolutionObserver _evolutionObserver;
    private long _lastTimestamp = 0;
    private EvolutionaryAlgorithmSettings _privateSettings;
    private bool _isRunning = false;
    private  Guid _solutionId = Guid.NewGuid();
    private Guid _lastSolutionId;
    private TimeSpan _time;
    
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
        
        var previewLevel = 0;
        if (!DA.GetData(2, ref previewLevel)) return;

        double timestamp = 0;
        if (!DA.GetData(3, ref timestamp)) return;
        long intTimestamp = Convert.ToInt64(timestamp);
        
        var run = false;
        if (!DA.GetData(4, ref run)) return;
        
            _evolutionObserver.Destroy();
        // Check if the solver was triggered by the button or timestamp
        if (run)
        {
            // Solver was triggered by the button
        }
        else if (intTimestamp != 0 && intTimestamp != _lastTimestamp)
        {
        }
        
        // Check if the solver should run
        if(_lastSolutionId != Guid.Empty && _solutionId != _lastSolutionId)
        {
            return;
        }
        
        // Set up state manager
        if (_stateManager.GetDocument() == null)
        {
            _stateManager.SetDocument(OnPingDocument());
            _stateManager.SetThisComponent(this);
            _stateManager.SetFitnessComponent();
        }
        
        _stateManager.SetGenes(geneIds);
        _stateManager.PreviewLevel = previewLevel;

        // Check if the solver should run
        bool start = (intTimestamp != 0 && intTimestamp != _lastTimestamp) || run;
        
        if(_stateManager.FitnessComponent == null)
        {
            _stateManager.SetThisComponent(this);
            _stateManager.SetFitnessComponent();

            if (_stateManager.FitnessComponent == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No fitness component found");
                return;
            }
        }

        // Run the solver
        if (start)
        {
            DA.SetData(0, null);
            _isRunning = true;
            OnPingDocument().ScheduleSolution(100, ScheduleCallback);
            _lastTimestamp = intTimestamp;
        }
    }
    
    /// <summary>
    /// Scedules the callback for the solver
    /// </summary>
    /// <param name="doc"></param>
    private void ScheduleCallback(GH_Document doc)
    {
        var start = DateTime.Now;
        _solutionId = Guid.NewGuid();
        _evolutionObserver.Reset();

        var evolutionaryAlgorithm = new EvolutionaryAlgorithm(_privateSettings, _stateManager, _evolutionObserver);

        evolutionaryAlgorithm.RunAlgorithm();

        OnPingDocument().ScheduleSolution(100, doc =>
        {
            _isRunning = false;
            ExpireSolution(false);
        });

        _lastSolutionId = _solutionId;
        var end = DateTime.Now;
        var time = end - start;
    }

    // /// <summary>
    // /// Clean the component
    // /// </summary>
    // protected override void AfterSolveInstance()
    // {
    //     base.AfterSolveInstance();
    //     if (_isRunning == false)
    //     {
    //         Params.Output[0].ClearData();
    //         Params.Output[0].AddVolatileData(new GH_Path(0), 0, _observer);
    //         Params.Output[1].ClearData();
    //         Params.Output[1].AddVolatileData(new GH_Path(0), 0, _stateManager);
    //     }
    //
    //     Params.Output[2].ClearData();
    //     Params.Output[2].AddVolatileData(new GH_Path(0), 0, _observer.CurrentGeneration);
    //     Params.Output[3].ClearData();
    //     Params.Output[3].AddVolatileData(new GH_Path(0), 0, _isRunning);
    // }

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