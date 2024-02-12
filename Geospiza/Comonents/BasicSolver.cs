using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using Geospiza.Lib;
using System.Threading.Tasks;

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
        pManager.AddTextParameter("GeneIds", "GID", "The gene ids", GH_ParamAccess.list);
        pManager.AddNumberParameter("Timestamp", "T", "The timestamp", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    List<GH_NumberSlider> numberSliders = new List<GH_NumberSlider>();
    List<dynamic> genepools = new List<dynamic>();
    public bool didRun = false;
    public int wasRunning = 0;
    public long lastTimestamp = 0;

    void ScheduleCallback(GH_Document doc)
    {
        GetGenes(doc);
        // ChangeSlider(doc);
        doc.ExpirePreview(false);
    }

     void GetGenes(GH_Document doc)
    {
        Random random = new Random();

        for (int j = 0; j < 50; j++)
        {
            if (numberSliders.Count != 0)
            {
                foreach (var slider in numberSliders)
                {
                    slider.TickValue = random.Next(0, slider.TickCount + 1);
                }
            }

            if (genepools.Count != 0)
            {
                foreach (var genepool in genepools)
                {
                    for (int i = 0; i < genepool.Count; i++)
                    {
                        var tickCount = genepool.TickCount;
                        var currentVal = random.Next(0, tickCount + 1);
                        genepool.set_TickValue(i, currentVal);
                    }
                        genepool.ExpireSolutionTopLevel(false);
                }
            }

            doc.NewSolution(false);
            
            System.Threading.Thread.Sleep(50);
        }
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
        
        double timestamp = 0;
        if (!DA.GetData(1, ref timestamp)) return;
        long intTimestamp = Convert.ToInt64(timestamp);
        
        var run = true;
        
        if (intTimestamp != lastTimestamp)
        {
            didRun = false;
            wasRunning = 0;
            numberSliders = new List<GH_NumberSlider>();
            genepools = new List<dynamic>();
        }

        if (intTimestamp == lastTimestamp)
        {
            run = false;
        }
    
        //Get the gene parameters
        foreach (var ids in geneIds)
        {
            var currentParam = this.OnPingDocument().FindObject(new Guid(ids), true);
            var currentType = currentParam.GetType().ToString();
            if (currentType == "GalapagosComponents.GalapagosGeneListObject")
            {
                genepools.Add(currentParam as dynamic);
            }
            else if (currentType == "Grasshopper.Kernel.Special.GH_NumberSlider")
            {
                numberSliders.Add(currentParam as GH_NumberSlider);
            }
        }

        //Run the solver
        if (run && didRun == false)
        {
            this.OnPingDocument().ScheduleSolution(10, ScheduleCallback);
            lastTimestamp = intTimestamp;
            didRun = true;

            if (numberSliders.Count == 0 || genepools.Count == 0)
            {
                return;
            }
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