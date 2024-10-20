using System.Net;
using System.Text;
using Newtonsoft.Json;
using GeospizaManager.Core;
using GeospizaManager.Strategies;

public class RequestContext
{
    public TaskCompletionSource<string> TaskCompletionSource { get; set; }
    public ReducedObserver ReducedObserver { get; set; }

    // Add any other individual parameters you need here
}

public class EvolutionCordinator
{
    private readonly HttpListener _listener;
    private readonly List<RequestContext> _requestContexts = new List<RequestContext>();
    private readonly object _dataLock = new object();
    private readonly int MaxRequests;

    public EvolutionCordinator(string[] prefixes, int maxRequests = 2)
    {
        MaxRequests = maxRequests;

        // Initialize the HttpListener with given prefixes (e.g., "http://localhost:8080/")
        _listener = new HttpListener();
        foreach (string prefix in prefixes)
        {
            _listener.Prefixes.Add(prefix);
        }
    }

    // Start listening for HTTP requests
    public async Task StartListeningAsync()
    {
        _listener.Start();
        Console.WriteLine("Listening for incoming POST requests...");

        // Run listener in a loop
        while (true)
        {
            HttpListenerContext context = await _listener.GetContextAsync();

            if (context.Request.HttpMethod == "POST")
            {
                _ = HandlePostRequestAsync(context);
            }
            else
            {
                // Respond with 405 Method Not Allowed
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                context.Response.Close();
            }
        }
    }

    // Stop the HTTP listener
    public void StopListening()
    {
        _listener.Stop();
    }

    private async Task HandlePostRequestAsync(HttpListenerContext context)
    {
        string requestBody;

        // Read the POST request body
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        ReducedObserver reducedObserver;

        try
        {
            // Deserialize the JSON into a ReducedObserver object
            reducedObserver = JsonConvert.DeserializeObject<ReducedObserver>(requestBody);

            Console.WriteLine($"Received data with {reducedObserver.Count} inhabitants.");
        }
        catch (JsonException ex)
        {
            // Handle JSON parsing error
            Console.WriteLine($"Error parsing JSON: {ex.Message}");

            // Respond with a 400 Bad Request status code
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            byte[] errorBuffer = Encoding.UTF8.GetBytes("Invalid JSON format.");
            context.Response.ContentLength64 = errorBuffer.Length;
            await context.Response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
            context.Response.OutputStream.Close();

            return;
        }

        TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();

        lock (_dataLock)
        {
            // Create and store the RequestContext
            var requestContext = new RequestContext
            {
                TaskCompletionSource = taskCompletionSource,
                ReducedObserver = reducedObserver,
                // HttpContext = context, // Not needed unless you need to access it later
                // Add any other individual parameters here
            };

            _requestContexts.Add(requestContext);

            // If we have enough requests, process the data
            if (_requestContexts.Count == MaxRequests)
            {
                ProcessDataAndRelease();
            }
        }

        // Wait for the processing result and respond
        string result = await taskCompletionSource.Task;

        byte[] buffer = Encoding.UTF8.GetBytes(result);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    private List<Individual> MergeInhabitants(List<ReducedObserver> observers)
    {
        List<Individual> allInhabitants = new List<Individual>();

        foreach (var observer in observers)
        {
            allInhabitants.AddRange(observer.Inhabitants);
        }

        return allInhabitants;
    }

    private List<Individual> RunEvolution(List<Individual> allInhabitants, int origialPopulationSize)
    {
        var newInhabitants = new List<Individual>();

        // Select the top individuals from the current population (elitism)
        var elite = Elitism.SelectTopIndividuals(origialPopulationSize, allInhabitants);
        newInhabitants.AddRange(elite);

        return newInhabitants;
    }

    private void ProcessDataAndRelease()
    {
        //TODO: Implement here the processing logic for the received data
        // Example processing: Merge all inhabitants from the received data
        var allInhabitants = MergeInhabitants(_requestContexts.Select(rc => rc.ReducedObserver).ToList());
        var newInhabitants = RunEvolution(allInhabitants, _requestContexts[0].ReducedObserver.Inhabitants.Count());


        var counter = 0;
        // Now, process each request individually
        foreach (var requestContext in _requestContexts)
        {
            // Access individual data
            var observer = requestContext.ReducedObserver;

            // Create a result object
            var result = new
            {
                TotalInhabitants = allInhabitants.Count,
                IndividualInhabitants = observer.Inhabitants.Count,
                IndividualParameter = counter, // Include individual parameter
                Message = "Data processed successfully."
            };

            // Serialize the result to JSON
            string individualResult = JsonConvert.SerializeObject(result);

            // Set the result for this task
            requestContext.TaskCompletionSource.SetResult(individualResult);
            counter++;
        }

        // Clear the stored data and request contexts
        _requestContexts.Clear();
    }
}