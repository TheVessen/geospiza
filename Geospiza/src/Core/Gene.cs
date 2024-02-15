﻿using System;
using Geospiza.Strategies;

namespace Geospiza.Core;

public class Gene
{   
    public int TickValue { get; private set; }
    public Guid GeneGuid { get; private set; }
    public int TickCount { get; private set; }
    
    public Gene(int tickValue, Guid geneGuid, int tickCount)
        {
            TickValue = tickValue;
            GeneGuid = geneGuid;
            TickCount = tickCount;
        }
    
    public Gene(Gene gene)
    {
        TickValue = gene.TickValue;
        GeneGuid = gene.GeneGuid;
        TickCount = gene.TickCount;
    }
    
    // This function should only be used from a mutation strategy
    public void MutatedValue(int mutation)
    {
        TickValue = mutation;
    }
    
}