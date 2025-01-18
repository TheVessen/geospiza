using Grasshopper.Kernel.Special;

namespace GeospizaManager.Core;

/// <summary>
/// Represents a template for a gene, which can be initialized with either a gene pool list or a number slider.
/// </summary>
public class GeneTemplate
{
  private dynamic? _genPoolList;
  private GH_NumberSlider? _slider;

  /// <summary>
  /// Initializes a new instance of the <see cref="GeneTemplate"/> class with a gene pool list and a gene index.
  /// </summary>
  /// <param name="genPoolList">The gene pool list.</param>
  /// <param name="geneIndex">The index of the gene in the gene pool.</param>
  public GeneTemplate(dynamic genPoolList, int geneIndex)
  {
    _genPoolList = genPoolList;
    GenePoolIndex = geneIndex;
    TickCount = genPoolList.TickCount;
    GeneGuid = Guid.NewGuid();
    Type = genPoolList.GetType();
    Name = genPoolList.NickName;
    GhInstanceGuid = genPoolList.InstanceGuid;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="GeneTemplate"/> class with a number slider.
  /// </summary>
  /// <param name="slider">The number slider.</param>
  public GeneTemplate(GH_NumberSlider slider)
  {
    _slider = slider;
    TickCount = slider.TickCount;
    GeneGuid = Guid.NewGuid();
    Type = slider.GetType();
    GhInstanceGuid = slider.InstanceGuid;
    Name = slider.NickName;
    GenePoolIndex = -1;
  }

  /// <summary>
  /// Gets or sets the tick value of the gene.
  /// </summary>
  public int TickValue { get; set; }

  /// <summary>
  /// Gets the type of the gene.
  /// </summary>
  private Type Type { get; }

  /// <summary>
  /// Gets the unique identifier for the GH instance.
  /// </summary>
  public Guid GhInstanceGuid { get; }

  /// <summary>
  /// Gets the unique identifier for the gene.
  /// </summary>
  public Guid GeneGuid { get; private set; }

  /// <summary>
  /// Gets the name of the gene.
  /// </summary>
  public string Name { get; private set; }

  /// <summary>
  /// Gets the tick count of the gene.
  /// </summary>
  public int TickCount { get; private set; }

  /// <summary>
  /// Gets the index of the gene in the gene pool.
  /// </summary>
  public int GenePoolIndex { get; }

  /// <summary>
  /// Sets the tick value for the gene and updates the corresponding slider or gene pool in the state manager.
  /// </summary>
  /// <param name="tickValue">The new tick value to set.</param>
  /// <param name="stateManager">The state manager containing all sliders and gene pools.</param>
  /// <exception cref="System.Exception">Thrown if the gene class has not been initialized with a dictionary of sliders and gene pools.</exception>
  public void SetTickValue(int tickValue, StateManager stateManager)
  {
    var allSliders = stateManager.AllSliders;
    var allGenePools = stateManager.AllGenePools;
    if (allSliders == null && allGenePools == null)
      throw new Exception("Gene class has not been initialized with a dictionary of sliders and gene pools.");

    if (Type == typeof(GH_NumberSlider))
    {
      if (allSliders != null) allSliders[GhInstanceGuid].TickValue = tickValue;
      TickValue = tickValue;
    }
    else
    {
      if (allGenePools != null) allGenePools[GhInstanceGuid].set_TickValue(GenePoolIndex, tickValue);
      TickValue = tickValue;
      if (allGenePools != null) allGenePools[GhInstanceGuid].ExpireSolutionTopLevel(false);
    }
  }
}