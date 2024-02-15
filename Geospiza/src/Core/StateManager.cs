using System;
using System.Collections.Generic;
using Geospiza.Comonents;
using Grasshopper.Kernel;

namespace Geospiza.Core;

public class StateManager
{
    private static StateManager _instance;
    private GH_Document _document;
    public GH_Component _thisComponent { get; private set; } = null;
    
    public Fitness FitnessComponent { get; private set; }
    public Dictionary<Guid, TemplateGene> TemplateGenes { get; private set; }
    
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
        foreach (var comp in _document.Objects)
        {
            if (comp is Fitness fitness)
            {
                FitnessComponent = fitness;
            }
        }

        if (FitnessComponent == null)
        {
            throw new Exception("No fitness component found in the document");
        }
    }

    public GH_Document GetDocument()
    {
        return _document;
    }
    
    public void SetGenes(List<string> geneIds)
    {
        if (TemplateGenes != null)
        {
            return;
        }
        TemplateGenes = Utils.InitializeGenePool(geneIds, _document);
    }
    
    public void SetThisComponent(GH_Component component)
    {
        if (_thisComponent != null)
        {
            return;
        }
        _thisComponent = component;
    }
    
}