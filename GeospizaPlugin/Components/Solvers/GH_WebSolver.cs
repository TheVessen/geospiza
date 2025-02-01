using System;
using System.Collections.Generic;
using System.Drawing;

using GeospizaCore.Core;
using GeospizaCore.Solvers;
using GeospizaPlugin.AsyncComponent;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace GeospizaPlugin.Components.Solvers
{
  public class GH_WebSolver : GH_WebsocketComponent
  {
    public GH_WebSolver()
        : base("Web Solver", "WS",
            "A solver that can be triggered via WebSocket (only when 'Activate' is true).",
            "Geospiza", "Solvers")
    {
      BaseWorker = new WebSolverWorker(this);
    }

    public override Guid ComponentGuid => new Guid("eee606f7-f3ac-4739-bcb2-a511ac58412a");

    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.Solver;

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
          "Activate",
          "A",
          "If TRUE, this component accepts 'run' commands over WebSocket.\nIf FALSE, remote triggers are ignored.",
          GH_ParamAccess.item,
          false
      );

      pManager.AddTextParameter(
          "Endpoint",
          "E",
          "WebSocket endpoint (optional). Currently not used to auto-restart the server, but can be used for display.",
          GH_ParamAccess.item,
          "ws://127.0.0.1:8181"
      );
      
      pManager.AddGenericParameter("WebIndividual", "WI", "The individual to be displayed in the web interface", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter(
          "Message",
          "M",
          "Status or results",
          GH_ParamAccess.item
      );
    }
    
    public class WebSolverWorker : WorkerInstance
    {
      private readonly object _lockObject = new object();
      public EvolutionObserver EvolutionObserver;
      public StateManager _stateManager;
      private bool _isRunning;
      private EvolutionBlueprint _blueprint;

      public WebSolverWorker(GH_Component parent) : base(parent)
      {
      }

      public override void DoWork(Action<string, double> ReportProgress, Action Done)
      {
        if (CancellationToken.IsCancellationRequested)
        {
          Done();
          return;
        }

        Parent.OnPingDocument().ScheduleSolution(100, ScheduleCallback);
        // Done();
      }

      private void ScheduleCallback(GH_Document doc)
      {
        lock (_lockObject)
        {
          if (_isRunning) return;
        }

        try
        {
          EvolutionObserver.Reset();
          _blueprint.RunAlgorithm();
        }
        finally
        {
          lock (_lockObject)
          {
            {
              _isRunning = false;
            }
          }

          doc.ScheduleSolution(100, doc2 => { doc2.NewSolution(true); });
        }
      }


      public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
      {
        var geneIds = new List<string>();
        if (!DA.GetDataList(0, geneIds)) return;

        var settings = new SolverSettings();
        if (!DA.GetData(1, ref settings)) return;

        var previewLevel = 0;
        if (!DA.GetData(2, ref previewLevel)) return;

        //Activation and endpoint are not used in this component. They are set in the parent class.

        var stateManager = StateManager.GetInstance(Parent, Parent.OnPingDocument());
        var evolutionObserver = EvolutionObserver.GetInstance(Parent);

        stateManager.SetGenes(geneIds);
        stateManager.PreviewLevel = previewLevel;

        lock (_lockObject)
        {
          _blueprint = new BaseSolver(settings, stateManager, evolutionObserver);
          _stateManager = stateManager;
          EvolutionObserver = evolutionObserver;
        }
      }


      public override void SetData(IGH_DataAccess DA)
      {
        string message;
        DA.SetData(0, "message");
      }


      public override WorkerInstance Duplicate() => new WebSolverWorker(Parent);
    }
  }
}


