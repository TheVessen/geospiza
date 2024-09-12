using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace GeospizaManager.GeospizaCordinator
{
    public class HttpServer : IDisposable
    {
        private static readonly Lazy<HttpServer> _instance = new Lazy<HttpServer>(() => new HttpServer());
        private readonly HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<HttpServer> _logger;
        private readonly SemaphoreSlim _maxConcurrentRequests;
        private readonly ConcurrentDictionary<Task, byte> _runningTasks = new ConcurrentDictionary<Task, byte>();

        private HttpServer()
        {
            _listener = new HttpListener();
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<HttpServer>();
            _maxConcurrentRequests = new SemaphoreSlim(Environment.ProcessorCount * 2); // Adjust as needed
        }

        public static HttpServer Instance => _instance.Value;

        public async Task StartAsync(string urlPrefix, Func<string, Task> onDataReceived)
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

        private async Task ListenForConnectionsAsync(Func<string, Task> onDataReceived, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _maxConcurrentRequests.WaitAsync(cancellationToken);
                    HttpListenerContext context = await _listener.GetContextAsync();
                    
                    var processTask = ProcessRequestAsync(context, onDataReceived, cancellationToken);
                    _ = TrackTaskCompletion(processTask);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested, break the loop
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting client connection");
                }
            }

            await Task.WhenAll(_runningTasks.Keys);
        }

        private async Task TrackTaskCompletion(Task task)
        {
            _runningTasks.TryAdd(task, 0);
            try
            {
                await task;
            }
            finally
            {
                _runningTasks.TryRemove(task, out _);
                _maxConcurrentRequests.Release();
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context, Func<string, Task> onDataReceived, CancellationToken cancellationToken)
        {
            try
            {
                if (context.Request.HttpMethod != "POST" || context.Request.Url.AbsolutePath != "/data")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                string json = await reader.ReadToEndAsync();
                string unescapedJson = JsonConvert.DeserializeObject<string>(json);

                await onDataReceived(unescapedJson);

                await SendResponseAsync(context.Response, "Data received and processed.", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                context.Response.Close();
            }
        }

        private async Task SendResponseAsync(HttpListenerResponse response, string message, CancellationToken cancellationToken)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        public async Task StopAsync()
        {
            if (_listener.IsListening)
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();
                await Task.WhenAll(_runningTasks.Keys);
                _logger.LogInformation("HTTP server stopped.");
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
            _listener.Close();
            _maxConcurrentRequests.Dispose();
        }
    }
}