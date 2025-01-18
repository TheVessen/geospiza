﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using GeospizaManager.GeospizaCordinator;
using GeospizaPlugin.Components.Solvers;
using Grasshopper.Kernel;

namespace GeospizaPlugin.Components.State;

public class StateManager : AsyncComponent
{
  /// <summary>
  /// Initializes a new instance of the StateManager class.
  /// </summary>
  public StateManager()
    : base("State Manager", "SM",
      "An async component that displays the state of the evolutionary algorithm",
      "Geospiza", "Utils")
  {
    BaseWorker = new EvoWorkerInstance();
  }

  /// <summary>
  /// Registers all the input parameters for this component.
  /// </summary>
  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddNumberParameter("Number", "N", "Description", GH_ParamAccess.item);
  }

  /// <summary>
  /// Registers all the output parameters for this component.
  /// </summary>
  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
  }

  /// <summary>
  /// Provides an Icon for the component.
  /// </summary>
  protected override Bitmap Icon =>
    //You can add image files to your project resources and access them like this:
    // return Resources.IconForThisComponent;
    null;

  /// <summary>
  /// Gets the unique ID for this component. Do not change this ID after release.
  /// </summary>
  public override Guid ComponentGuid => new("C467D430-2667-440C-B95F-B93C2C26D667");


  private class EvoWorkerInstance : WorkerInstance
  {
    public EvoWorkerInstance() : base(null)
    {
    }

    public double Num { get; set; }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      var num = 0.0;
      if (!DA.GetData(0, ref num)) return;

      Num = num;
    }


    public override WorkerInstance Duplicate()
    {
      return new EvoWorkerInstance();
    }

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      if (CancellationToken.IsCancellationRequested) return;

      var refrenceKey = new Guid();

      Task.Run(() => { return DataSender.SendDataAsync(jsonObject, refrenceKey); }).ContinueWith((task) =>
      {
        if (task.Result != null) Console.WriteLine($"In Worker {refrenceKey.ToString()} res is {task.Result}");

        Done();
      });
    }

    public override void SetData(IGH_DataAccess DA)
    {
    }

    private string jsonObject =
      "{\n  \"CurrentGenerationIndex\": 11,\n  \"RequestId\": \"68952650-fb8e-4f89-9a36-69f791704a68\",\n  \"Inhabitants\": [\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 32,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    },\n    {\n      \"GenePool\": [\n        {\n          \"TickValue\": 54,\n          \"GeneGuid\": \"e6d21964-22c0-48fe-be85-a5f0b0c61c84\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 42,\n          \"GeneGuid\": \"e330bdb0-f723-4794-ae86-084f32c0c845\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 48,\n          \"GeneGuid\": \"598e631d-0c83-42f2-bf9c-0000bd332696\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 53,\n          \"GeneGuid\": \"c5330937-c65f-45b5-89d2-4a0e1e8de07e\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 50,\n          \"GeneGuid\": \"2e9fd207-3335-46db-bfed-ad2a86b4f178\",\n          \"TickCount\": 60,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"06a632d4-cb15-425c-9fd7-52b75a748da7\",\n          \"GenePoolIndex\": 4\n        },\n        {\n          \"TickValue\": 5,\n          \"GeneGuid\": \"1c6776c9-2707-4c74-9c23-092cda7b316a\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 0\n        },\n        {\n          \"TickValue\": 4,\n          \"GeneGuid\": \"c4b0dd48-e06d-4956-8e14-6dc98e069696\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 1\n        },\n        {\n          \"TickValue\": 0,\n          \"GeneGuid\": \"c969408f-3e06-4ca8-a075-5f8a2d9151fb\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 2\n        },\n        {\n          \"TickValue\": 1,\n          \"GeneGuid\": \"c3a622b3-434f-48a9-a4e4-f87079edf74b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 3\n        },\n        {\n          \"TickValue\": 2,\n          \"GeneGuid\": \"11f8ee41-52d8-40d0-a608-4fa0eada371b\",\n          \"TickCount\": 6,\n          \"GeneName\": \"wlc_Floors\",\n          \"GhInstanceGuid\": \"2c99e319-3a38-42f2-82b0-45055143b986\",\n          \"GenePoolIndex\": 4\n        }\n      ],\n      \"Fitness\": 61.1091044601114,\n      \"Probability\": 0.0,\n      \"Generation\": 11\n    }\n  ],\n  \"Count\": 10\n}\n";
  }
}