using System;
using System.Collections.Generic;
using Geospiza.Comonents;
using Grasshopper.Kernel;

namespace Geospiza.Core;

public class StateManager
{
    private static StateManager _instance;
    private GH_Document _document;
    public GH_Component ThisComponent { get; private set; } = null;
    
    public Fitness FitnessComponent { get; private set; }
    public Dictionary<Guid, TemplateGene> Genotype { get; private set; }
    
    private StateManager() { }

    public static StateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new StateManager();
            }
            return _instance;
        }
    }

    public void SetDocument(GH_Document document)
    {
        if(_document != null)
        {
            return;
        }
        
        _document = document;
    }
    
    public GH_Document GetDocument()
    {
      return _document;
    }
    
    public void SetFitnessComponent()
    {
      
        if (FitnessComponent != null)
        {
            return;
        }
        
        foreach (var comp in _document.Objects)
        {
          if (comp is Fitness fitness)
          {
            FitnessComponent = fitness;
          }
        }
    }

    public void SetGenes(List<string> geneIds)
    {
        if (Genotype.Count == geneIds.Count)
        {
            return;
        }
        
        Reset();

        Genotype = Utils.InitializeGenePool(geneIds, _document);
    }
    
    private void Reset()
    {
        FitnessComponent = null;
        Genotype = null;
        ThisComponent = null;
    }
    
    public void SetThisComponent(GH_Component component)
    {
        if (ThisComponent != null)
        {
            return;
        }
        ThisComponent = component;
    }
}