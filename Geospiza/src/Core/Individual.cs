using System;
using System.Collections.Generic;

namespace Geospiza.Core;

public class Individual
{
    public List<Gene> _genePool { get; private set; }
    public double Fitness { get; private set; }
    
    public void AddStableGene(Gene gene)
    {
        _genePool.Add(gene);
    }
}