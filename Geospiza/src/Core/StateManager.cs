using System;
using System.Collections.Generic;
using Geospiza.Comonents;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Geospiza.Core;

public class StateManager
{
    private static StateManager _instance;
    private GH_Document _document;
    public GH_Component ThisComponent { get; private set; } = null;
    public int NumberOfGeneIds { get;set; } = 0;
    
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
        if (Genotype == null)
        {
            Genotype = InitializeGenePool(geneIds, _document);
            NumberOfGeneIds = geneIds.Count;
        } else if (NumberOfGeneIds != geneIds.Count)
        {
            Reset();
            NumberOfGeneIds = geneIds.Count;
            Genotype = InitializeGenePool(geneIds, _document);
        }
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
    
     /// <summary>
    /// Initializes a gene pool from a list of gene IDs and a GH_Document.
    /// </summary>
    /// <param name="geneIds">A list of gene IDs to initialize the gene pool with.</param>
    /// <param name="doc">The GH_Document to use for finding objects.</param>
    /// <returns>A dictionary of genes where the key is a Guid and the value is a Gene object.</returns>
    private static Dictionary<Guid, TemplateGene> InitializeGenePool(List<string> geneIds, GH_Document doc)
    {
        var genes = new Dictionary<Guid, TemplateGene>();
        var genePools = new Dictionary<Guid, dynamic>();
        var numberSliders = new Dictionary<Guid, GH_NumberSlider>();
        
        foreach (var id in geneIds)
        {
            var currentParam = doc.FindObject(new Guid(id), true);
            var currentType = currentParam.GetType().ToString();
            var guid = currentParam.InstanceGuid;

            switch (currentType)
            {
                case "GalapagosComponents.GalapagosGeneListObject":
                {
                    var genePool = (dynamic)currentParam;
                    genePools[guid] = genePool;
                    if (genePool.Count != 0)
                    {
                        for (var i = 0; i < genePool.Count; i++)
                        {
                            var gene = new TemplateGene(genePool, i);
                            genes[gene.GeneGuid] = gene;
                        }
                    }

                    break;
                }
                case "Grasshopper.Kernel.Special.GH_NumberSlider":
                {
                    var sliderGene = currentParam as GH_NumberSlider;
                    numberSliders[guid] = sliderGene;
                    var gene = new TemplateGene(sliderGene);
                    genes[gene.GeneGuid] = gene;
                    break;
                }
            }
        }
        new TemplateGene(numberSliders, genePools);
        return genes;
    }
}