using System;
using System.Collections.Generic;

namespace Geospiza.Core;

public static class Utils
{
  /// <summary>
  ///   This method is used to create a snapshot of the current state of genes.
  /// </summary>
  /// <param name="genes">A dictionary of genes where the key is a Guid and the value is a Gene object.</param>
  /// <returns>A dictionary of stable genes where the key is a Guid and the value is a StableGene object.</returns>
  public static Dictionary<Guid, Gene> GetGeneSnapshot(Dictionary<Guid, GeneTemplate> genes)
  {
    var stableGenes = new Dictionary<Guid, Gene>();
    foreach (var gene in genes)
      stableGenes[gene.Key] = new Gene(gene.Value.TickValue, gene.Key, gene.Value.TickCount, gene.Value.Name,
        gene.Value.GhInstanceGuid, gene.Value.GenePoolIndex);
    return stableGenes;
  }
}