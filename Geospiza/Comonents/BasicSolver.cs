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

namespace Geospiza.Comonents;

public class BasicSolver : GH_Component
{
    /// <summary>
    /// Initializes a new instance of the BasicSolver class.
    /// </summary>
    public BasicSolver()
        : base("BasicSolver", "BS",
            "Description",
            "Geospiza", "Solvers")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("GeneIds", "GID", "The gene ids from the GeneSelector", GH_ParamAccess.list);
        pManager.AddNumberParameter("Timestamp", "T", "Timestamp from the server to determine if the solver should run",
            GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("CurrentGeneration", "CG", "The current generation", GH_ParamAccess.item);
    }

    private bool _didRun = false;
    private long _lastTimestamp = 0;

    void ScheduleCallback(GH_Document doc)
    {
        
        var evolutionaryAlgorithm = new EvolutionaryAlgorithm();
        
        evolutionaryAlgorithm.RunAlgorithm();
        
        // var random = new Random();
        //
        // Fitness fitti = null;
        //
        // foreach (var c in doc.Objects)
        // {
        //     if (c is Fitness fitness)
        //     {
        //         fitti = fitness;
        //     }
        // }
        //
        // if (fitti == null)
        // {
        //     this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No fitness component found. Please add a fitness component to the canvas.");
        //     return;
        // }
        //
        // for (var i = 0; i < 100; i++)
        // {
        //     
        //     foreach (var gene in _allGenes)
        //     {
        //         var currentGene = gene.Value;
        //
        //         currentGene.SetTickValue(random.Next(currentGene.TickCount + 1));
        //
        //     }
        //     Params.Output[0].AddVolatileData(new GH_Path(0), 0, fitti.FitnessValue);
        //     doc.NewSolution(false);
        // }
        doc.ExpirePreview(false);

    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        //Get inputs
        var geneIds = new List<string>();
        if (!DA.GetDataList(0, geneIds)) return;

        //Timestamp to determine if the solver should run
        double timestamp = 0;
        if (!DA.GetData(1, ref timestamp)) return;
        long intTimestamp = Convert.ToInt64(timestamp);
        
        
        //Check if the solver should run
        var run = true;

        if (intTimestamp != _lastTimestamp)
        {
            _didRun = false;
        }

        if (intTimestamp == _lastTimestamp)
        {
            run = false;
        }

        StateManager stateManager = StateManager.Instance;
        stateManager.SetDocument(OnPingDocument());
        stateManager.SetGenes(geneIds);
        stateManager.SetThisComponent(this);

        //Run the solver
        if (run && _didRun == false)
        {
            OnPingDocument().ScheduleSolution(10, ScheduleCallback);
            _lastTimestamp = intTimestamp;
            _didRun = true;
        }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon
    {
        get
        {
            //You can add image files to your project resources and access them like this:
            // return Resources.IconForThisComponent;
            return null;
        }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
        get { return new Guid("DC3BBA6C-488E-496C-AE62-5488B065C38F"); }
    }
}