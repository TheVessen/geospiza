
using GeospizaManager.Core;
using GeospizaManager.GeospizaCordinator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var coordinator = EvolutionarySolverCoordinator.Instance("Test", 1);
        coordinator.StartHttpServer();

        var individual = new Individual();
        individual.SetGeneration(10);
        individual.SetFitness(0.5);

        string json = individual.ToJson();
        Individual deserializedIndividual = Individual.FromJson(json);
        
        
        await DataSender.SendDataAsync(json);
        
        coordinator.StopHttpServer();

    }
}