using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Geospiza.Core;

public static class Utils
{
    /// <summary>
    /// Initializes a gene pool from a list of gene IDs and a GH_Document.
    /// </summary>
    /// <param name="geneIds">A list of gene IDs to initialize the gene pool with.</param>
    /// <param name="doc">The GH_Document to use for finding objects.</param>
    /// <returns>A dictionary of genes where the key is a Guid and the value is a Gene object.</returns>
    public static Dictionary<Guid, TemplateGene> InitializeGenePool(List<string> geneIds, GH_Document doc)
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
        
        //Initialize gene => Sets all the number sliders and gene pools inside the gene class
        new TemplateGene(numberSliders, genePools);
        
        return genes;
    }
    
    /// <summary>
    /// This method is used to create a snapshot of the current state of genes.
    /// </summary>
    /// <param name="genes">A dictionary of genes where the key is a Guid and the value is a Gene object.</param>
    /// <returns>A dictionary of stable genes where the key is a Guid and the value is a StableGene object.</returns>
    public static Dictionary<Guid, Gene> GetGeneSnapshot(Dictionary<Guid, TemplateGene> genes)
    {
        var stableGenes = new Dictionary<Guid, Gene>();
        foreach (var gene in genes)
        {
            stableGenes[gene.Key] = new Gene(gene.Value.TickValue, gene.Key, gene.Value.TickCount, gene.Value.Name);
        }
        return stableGenes;
    }
}