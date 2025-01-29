using System.Net;
using System.Text;
using GeospizaCore.Core;
using GeospizaCore.Strategies;
using Newtonsoft.Json;

namespace GeospizaCore.ParallelSpiza;

///ATTENTION This is still very much in the works. More documentation  will be added as the project progresses.
public class RequestContext
{
    public TaskCompletionSource<string> TaskCompletionSource { get; set; }
    public ObserverServerSnapshot? BaseObserver { get; set; }
}

public class EvolutionCordinator
{
    private readonly HttpListener _listener;
    private readonly List<RequestContext> _requestContexts = new();
    private readonly object _dataLock = new();
    private readonly int _maxRequests;

    public EvolutionCordinator(string[] prefixes, int maxRequests = 2)
    {
        _maxRequests = maxRequests;
        _listener = new HttpListener();
        foreach (var prefix in prefixes) _listener.Prefixes.Add(prefix);
    }

    /// <summary>
    /// Starts the HTTP listener and processes incoming POST requests asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Console.WriteLine("Listening for incoming POST requests...");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                if (context.Request.HttpMethod == "POST")
                {
                    _ = HandlePostRequestAsync(context);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    context.Response.Close();
                }
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 995)
        {
            Console.WriteLine("Listener stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            _listener.Stop();
        }
    }

    public void StopListening()
    {
        _listener.Stop();
    }

    private async Task HandlePostRequestAsync(HttpListenerContext context)
    {
        string requestBody;

        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        ObserverServerSnapshot? baseObserver;

        try
        {
            // Deserialize the JSON into a BaseObserver object
            baseObserver = JsonConvert.DeserializeObject<ObserverServerSnapshot>(requestBody);

            Console.WriteLine($"Received data with {baseObserver.Count} inhabitants.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing JSON: {ex.Message}");

            // Respond with a 400 Bad Request status code
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var errorBuffer = Encoding.UTF8.GetBytes("Invalid JSON format.");
            context.Response.ContentLength64 = errorBuffer.Length;
            await context.Response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
            context.Response.OutputStream.Close();

            return;
        }

        var taskCompletionSource = new TaskCompletionSource<string>();

        lock (_dataLock)
        {
            // Create and store the RequestContext
            var requestContext = new RequestContext
            {
                TaskCompletionSource = taskCompletionSource,
                BaseObserver = baseObserver
            };

            _requestContexts.Add(requestContext);

            // If we have enough requests, process the data
            if (_requestContexts.Count == _maxRequests) ProcessDataAndRelease();
        }

        var result = await taskCompletionSource.Task;
        var buffer = Encoding.UTF8.GetBytes(result);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    private List<Individual> MergeInhabitants(List<ObserverServerSnapshot> observers)
    {
        var allInhabitants = new List<Individual>();
        foreach (var observer in observers) allInhabitants.AddRange(observer.Inhabitants);

        return allInhabitants;
    }

    private List<Individual> RunEvolution(List<Individual> allInhabitants, int origialPopulationSize)
    {
        var newInhabitants = new List<Individual>();
        var elite = Elitism.SelectTopIndividuals(origialPopulationSize, allInhabitants);
        newInhabitants.AddRange(elite);

        return newInhabitants;
    }

    private void ProcessDataAndRelease()
    {
        //TODO: Implement here the processing logic for the received data
        // Example processing: Merge all inhabitants from the received data
        var allInhabitants = MergeInhabitants(_requestContexts.Select(rc => rc.BaseObserver).ToList());
        var newInhabitants = RunEvolution(allInhabitants, _requestContexts[0].BaseObserver.Inhabitants.Count());


        var counter = 0;
        // Now, process each request individually
        foreach (var requestContext in _requestContexts)
        {
            var observer = requestContext.BaseObserver;

            var result = new
            {
                TotalInhabitants = allInhabitants.Count,
                IndividualInhabitants = observer.Inhabitants.Count,
                IndividualParameter = counter, // Include individual parameter
                Message = "Data processed successfully."
            };

            var individualResult = JsonConvert.SerializeObject(result);
            requestContext.TaskCompletionSource.SetResult(individualResult);
            counter++;
        }

        _requestContexts.Clear();
    }
}