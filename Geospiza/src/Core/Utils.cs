using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Geospiza.Core;

public static class Utils
{
    public static Dictionary<Guid, Gene> InitializeGenePool(List<string> geneIds, GH_Document doc)
    {
        Dictionary<Guid, Gene> genes = new Dictionary<Guid, Gene>();
        Dictionary<Guid, dynamic> genePools = new Dictionary<Guid, dynamic>();
        Dictionary<Guid, GH_NumberSlider> numberSliders = new Dictionary<Guid, GH_NumberSlider>();
        
        foreach (var id in geneIds)
        {
            var currentParam = doc.FindObject(new Guid(id), true);
            var currentType = currentParam.GetType().ToString();

            Guid guid = currentParam.InstanceGuid;

            if (currentType == "GalapagosComponents.GalapagosGeneListObject")
            {
                var genePool = currentParam as dynamic;
                genePools[guid] = genePool;
                if (genePool.Count != 0)
                {
                    for (int i = 0; i < genePool.Count; i++)
                    {
                        var gene = new Gene(genePool, i);
                        genes[gene.GeneGuid] = gene;
                    }
                }
            }
            else if (currentType == "Grasshopper.Kernel.Special.GH_NumberSlider")
            {
                var sliderGene = currentParam as GH_NumberSlider;
                numberSliders[guid] = sliderGene;
                var gene = new Gene(sliderGene);
                genes[gene.GeneGuid] = gene;
            }
        }
        
        //Initalize gene => Sets all the number sliders and gene pools inside the gene class
        var baseGene = new Gene(numberSliders, genePools);
        
        return genes;
    }
    
}