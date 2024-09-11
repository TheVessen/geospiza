
using GeospizaManager.GeospizaCordinator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var coordinator = EvolutionarySolverCoordinator.Instance("Test", 1);
        coordinator.StartHttpServer();

        var data = new
        {
            pro1 = "dhowsiehpf"
        };

        await DataSender.SendDataAsync(data);
        
        
        coordinator.StopHttpServer();

    }
}