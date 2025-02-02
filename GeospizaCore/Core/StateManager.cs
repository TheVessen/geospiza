using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GeospizaCore.Core;

/// <summary>
/// Manages the state of the local application, including the gene pools and sliders. It is grasshopper file specific.
/// </summary>
public class StateManager
{
    /// <summary>
    /// A dictionary that holds instances of StateManager for each GH_Component.
    /// </summary>
    private static readonly Dictionary<GH_Component, StateManager> Instances = new();

    /// <summary>
    /// A dictionary that holds all gene pools, where the key is a Guid and the value is a dynamic object representing the gene pool.
    /// </summary>
    public Dictionary<Guid, dynamic> AllGenePools { get; private set; } = new();

    /// <summary>
    /// A dictionary that holds all number sliders, where the key is a Guid and the value is a GH_NumberSlider object.
    /// </summary>
    public Dictionary<Guid, GH_NumberSlider> AllSliders { get; private set; } = new();

    /// <summary>
    /// The GH_Document associated with the StateManager.
    /// </summary>
    private readonly GH_Document _document;

    /// <summary>
    /// The Solver associated with the StateManager.
    /// </summary>
    private GH_Component SolverComponent { get; }

    /// <summary>
    /// The number of gene IDs managed by the StateManager.
    /// </summary>
    private int NumberOfGeneIds { get; set; }

    private int _previewLevel;
    
    public bool IsRunning { get; set; }

    /// <summary>
    /// The preview level for the StateManager. Valid values are:
    /// 0 - All Individuals are shown (default)
    /// 1 - EveryGeneration (only the best individual of each generation is shown)
    /// 2 - IfBetter (only individuals with a better fitness than the previous best are shown)
    /// 3 - None (no individuals are shown)
    /// If an invalid value is provided, it defaults to 0 (All).
    /// </summary>
    public int PreviewLevel
    {
        get => _previewLevel;
        set => _previewLevel = value is < 0 or > 3 ? 0 : value;
    }

    /// <summary>
    /// The singleton instance of the Fitness class.
    /// </summary>
    private Fitness FitnessSingleton { get; set; } = Fitness.Instance;

    /// <summary>
    /// The fitness component associated with the StateManager.
    /// </summary>
    public GH_Component FitnessComponent { get; private set; }
    
    /// <summary>
    /// A dictionary that holds the genotype, where the key is a Guid and the value is a GeneTemplate object.
    /// </summary>
    public Dictionary<Guid, GeneTemplate> Genotype { get; private set; } = new();

    /// <summary>
    /// An object used for locking to ensure thread safety.
    /// </summary>
    private static readonly object Padlock = new();

    private StateManager(GH_Component component, GH_Document document, GH_Component fitnessComponent)
    {
        SolverComponent = component ?? throw new ArgumentNullException(nameof(component));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        FitnessComponent = fitnessComponent;
    }

    /// <summary>
    /// Returns the instance of StateManager for the given solver.
    /// </summary>
    public static StateManager GetInstance(GH_Component solver, GH_Document document)
    {
        var foundComponent = document.Objects
            .OfType<GH_Component>()
            .FirstOrDefault(comp => comp.GetType().Name == "GH_Fitness");
        
        var webIndividualComponents = document.Objects
            .OfType<GH_Component>()
            .Where(comp => comp.GetType().Name == "GH_WebIndividual")
            .ToList();

        if (foundComponent == null)
        {
            solver.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Fitness component not found");
            return null;
        }

        // Lock the Instances dictionary to ensure thread safety.
        lock (Padlock)
        {
            if (!Instances.TryGetValue(solver, out var instance))
            {
                instance = new StateManager(solver, document, foundComponent);
                Instances[solver] = instance;
            }

            return instance;
        }
    }

    /// <summary>
    /// Returns the document of the StateManager.
    /// </summary>
    public GH_Document GetDocument()
    {
        return _document;
    }

    /// <summary>
    /// Sets the genes for the StateManager.
    /// </summary>
    /// <summary>
    /// Sets the genes for the StateManager.
    /// </summary>
    public void SetGenes(List<string> geneIds)
    {
        if (NumberOfGeneIds != geneIds.Count)
        {
            Reset();
            NumberOfGeneIds = geneIds.Count;
            SetGenePool(geneIds);
        }
    }
    
    public double GetFitness()
    {
        return FitnessSingleton.GetFitness();
    }

    /// <summary>
    /// Resets the StateManager to its initial state.
    /// </summary>
    private void Reset()
    {
        FitnessSingleton.ResetFitness();
        Genotype.Clear();
        AllSliders.Clear();
        AllGenePools.Clear();
    }

    /// <summary>
    /// Initializes a gene pool from a list of gene IDs and a GH_Document.
    /// </summary>
    /// <param name="geneIds">A list of gene IDs to initialize the gene pool with.</param>
    /// <returns>A dictionary of genes where the key is a Guid and the value is a Gene object.</returns>
    private void SetGenePool(List<string> geneIds)
    {
        var genes = new Dictionary<Guid, GeneTemplate>();
        var genePools = new Dictionary<Guid, dynamic>();
        var numberSliders = new Dictionary<Guid, GH_NumberSlider>();

        foreach (var id in geneIds)
        {
            var currentParam = _document.FindObject(new Guid(id), true);
            var currentType = currentParam.GetType().ToString();
            var guid = currentParam.InstanceGuid;

            switch (currentType)
            {
                case "GalapagosComponents.GalapagosGeneListObject":
                {
                    var genePool = (dynamic)currentParam;
                    genePools[guid] = genePool;
                    if (genePool.Count != 0)
                        for (var i = 0; i < genePool.Count; i++)
                        {
                            var gene = new GeneTemplate(genePool, i);
                            genes[gene.GeneGuid] = gene;
                        }

                    break;
                }
                case "Grasshopper.Kernel.Special.GH_NumberSlider":
                {
                    var sliderGene = currentParam as GH_NumberSlider;
                    if (sliderGene != null) numberSliders[guid] = sliderGene;

                    if (sliderGene != null)
                    {
                        var gene = new GeneTemplate(sliderGene);
                        genes[gene.GeneGuid] = gene;
                    }

                    break;
                }
            }
        }

        Genotype = genes;
        AllSliders = numberSliders;
        AllGenePools = genePools;
    }
}