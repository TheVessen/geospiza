
using GeospizaManager.Core;
using GeospizaManager.GeospizaCordinator;

public class Program
{
    private static Dictionary<Task, TaskCompletionSource<string>> _taskCompletionSources = new Dictionary<Task, TaskCompletionSource<string>>();
    public static async Task Main(string[] args)
    {
        var individual = new Individual();
        individual.SetGeneration(10);
        individual.SetFitness(0.5);
        var json = individual.ToJson();

        var coordinator = EvolutionarySolverCoordinator.Instance("Test", 3);
        coordinator.DataProcessed += OnDataProcessed; 
        coordinator.StartHttpServer();
        
        Console.WriteLine("Server started. Press Enter to stop the server...");

        var data = await DataSender.SendDataAsync(json, new Guid());
        Console.WriteLine($"The data was {data}");

        // Wait for the user to press Enter
        Console.ReadLine();

        coordinator.StopHttpServer();
    }
    
    private static void OnDataProcessed(string result)
    {
        // Handle the notification
        Console.WriteLine($"Data processed: {result}");
    }
}