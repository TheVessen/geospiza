using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GeospizaManager.Core
{
    /// <summary>
    /// Manages the state of the application, including the gene pools and sliders. It is grasshopper file specific.
    /// </summary>
    public class StateManager
    {
        // Stores instances of StateManager for each GH_BasicSolver
        private static readonly Dictionary<GH_Component, StateManager> Instances = new();
        public Dictionary<Guid, dynamic>? AllGenePools;
        public Dictionary<Guid, GH_NumberSlider>? AllSliders;
        private GH_Document? _document;
        public GH_Component? ThisComponent { get; private set; }
        public int NumberOfGeneIds { get; set; }
        public int PreviewLevel { get; set; } = 0;
        public Fitness? FitnessSingleton { get; private set; }
        public GH_Component? FitnessComponent { get; private set; }
        public Dictionary<Guid, GeneTemplate>? Genotype { get; private set; }
        private static readonly object Padlock = new object();


        private StateManager()
        {
        }

        /// <summary>
        /// Returns the instance of StateManager for the given solver.
        /// </summary>
        public static StateManager GetInstance(GH_Component solver)
        {
            lock (Padlock)
            {
                if (!Instances.ContainsKey(solver)) Instances[solver] = new StateManager();
                return Instances[solver];
            }
        }

        /// <summary>
        /// Sets the document for the StateManager.
        /// </summary>
        public void SetDocument(GH_Document document)
        {
            if (_document != null) return;

            _document = document;
        }

        /// <summary>
        /// Returns the document of the StateManager.
        /// </summary>
        public GH_Document? GetDocument()
        {
            return _document;
        }

        /// <summary>
        /// Sets the fitness component for the StateManager.
        /// </summary>
        public void SetFitnessComponent()
        {
            if (FitnessSingleton != null) return;

            if (_document != null && _document.Objects != null)
            {
                foreach (var comp in _document.Objects)
                {
                    if (comp.GetType().Name == "GH_Fitness")
                    {
                        FitnessComponent = (GH_Component)comp;
                        break;
                    }
                }
            }

            if (FitnessComponent == null)
            {
                ThisComponent?.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No fitness component found");
            }
        }

        /// <summary>
        /// Sets the genes for the StateManager.
        /// </summary>
        public void SetGenes(List<string> geneIds)
        {
            if (Genotype == null)
            {
                if (_document != null) InitializeGenePool(geneIds, _document);
                NumberOfGeneIds = geneIds.Count;
            }
            else if (NumberOfGeneIds != geneIds.Count)
            {
                Reset();
                NumberOfGeneIds = geneIds.Count;
                if (_document != null) InitializeGenePool(geneIds, _document);
            }
        }

        /// <summary>
        /// Resets the StateManager.
        /// </summary>
        private void Reset()
        {
            FitnessSingleton = null;
            Genotype = null;
            ThisComponent = null;
            AllSliders = null;
            AllGenePools = null;
        }

        /// <summary>
        /// Sets the component for the StateManager.
        /// </summary>
        public void SetThisComponent(GH_Component component)
        {
            if (ThisComponent != null) return;
            ThisComponent = component;
        }

        /// <summary>
        /// Initializes a gene pool from a list of gene IDs and a GH_Document.
        /// </summary>
        /// <param name="geneIds">A list of gene IDs to initialize the gene pool with.</param>
        /// <param name="doc">The GH_Document to use for finding objects.</param>
        /// <returns>A dictionary of genes where the key is a Guid and the value is a Gene object.</returns>
        private void InitializeGenePool(List<string> geneIds, GH_Document doc)
        {
            var genes = new Dictionary<Guid, GeneTemplate>();
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
                        if (sliderGene != null)
                        {
                            numberSliders[guid] = sliderGene;
                        }
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
}