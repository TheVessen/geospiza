using GeospizaManager.Core;
using GeospizaManager.GeospizaCordinator;

/// <summary>
/// !ATENTION! This is still very much in the works. Idea is to have a server managing multiple instances of Geospiza.
/// </summary>

public class Program
{
  private static Dictionary<Task, TaskCompletionSource<string>> _taskCompletionSources =
    new Dictionary<Task, TaskCompletionSource<string>>();

  public static async Task Main(string[] args)
  {
    // Define the prefixes (e.g., the URL and port the HTTP listener will listen to)
    string[] prefixes = { "http://localhost:8080/" };

    // Create an instance of the PostRequestHandler class
    var postRequestHandler = new EvolutionCordinator(prefixes, 2);
    var cancellationTokenSource = new CancellationTokenSource();

    // Start the listener and process incoming requests
    await postRequestHandler.StartListeningAsync(cancellationTokenSource.Token);
    
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
  }

  private static void OnDataProcessed(string result)
  {
    // Handle the notification
    Console.WriteLine($"Data processed: {result}");
  }
}