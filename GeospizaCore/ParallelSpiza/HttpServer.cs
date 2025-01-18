using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GeospizaManager.GeospizaCordinator;

public class HttpServer : IDisposable
{
  private static readonly Lazy<HttpServer> _instance = new(() => new HttpServer());
  private readonly HttpListener _listener;
  private CancellationTokenSource _cancellationTokenSource;
  private readonly ILogger<HttpServer> _logger;

  private HttpServer()
  {
    _listener = new HttpListener();
    _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<HttpServer>();
  }

  public static HttpServer Instance => _instance.Value;

  public async Task StartAsync(string urlPrefix, Func<string, Task<string>> onDataReceived)
  {
    if (_listener.IsListening)
    {
      _logger.LogWarning("Server is already running.");
      return;
    }

    _cancellationTokenSource = new CancellationTokenSource();
    _listener.Prefixes.Add(urlPrefix);
    _listener.Start();
    _logger.LogInformation($"Listening for connections on {urlPrefix}");

    await ListenForConnectionsAsync(onDataReceived, _cancellationTokenSource.Token);
  }

  private async Task ListenForConnectionsAsync(Func<string, Task<string>> onDataReceived,
    CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
      try
      {
        var context = await _listener.GetContextAsync();
        if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/server-info")
        {
          await ProcessServerInfoRequestAsync(context, cancellationToken);
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/data")
        {
          Console.WriteLine("Data received");
          await ProcessDataRequestAsync(context, onDataReceived, cancellationToken);
        }
        else
        {
          context.Response.StatusCode = (int)HttpStatusCode.NotFound;
          context.Response.Close();
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error accepting client connection");
      }
  }

  private async Task ProcessServerInfoRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
  {
    try
    {
      var serverInfo = new
      {
        Status = "Running",
        UrlPrefixes = _listener.Prefixes
      };

      var json = JsonConvert.SerializeObject(serverInfo);
      await SendResponseAsync(context.Response, json, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing server info request");
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    }
    finally
    {
      context.Response.Close();
    }
  }

  private async Task ProcessDataRequestAsync(HttpListenerContext context, Func<string, Task<string>> onDataReceived,
    CancellationToken cancellationToken)
  {
    try
    {
      using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
      var json = await reader.ReadToEndAsync();
      var result = await onDataReceived(json);
      await SendResponseAsync(context.Response, result, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing data request");
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    }
    finally
    {
      context.Response.Close();
    }
  }

  private async Task SendResponseAsync(HttpListenerResponse response, string message,
    CancellationToken cancellationToken)
  {
    var buffer = Encoding.UTF8.GetBytes(message);
    response.ContentLength64 = buffer.Length;
    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
    Console.WriteLine("Data received");
  }

  public async Task StopAsync()
  {
    if (_listener.IsListening)
    {
      _cancellationTokenSource.Cancel();
      _listener.Stop();
      _logger.LogInformation("HTTP server stopped.");
    }
  }

  public void Dispose()
  {
    StopAsync().GetAwaiter().GetResult();
    _cancellationTokenSource?.Dispose();
    _listener.Close();
  }
}