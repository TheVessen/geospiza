using System.Net;
using System.Text;

namespace GeospizaManager.GeospizaCordinator;

public class HttpServer
{
    private static HttpServer _instance;
    private static readonly object Padlock = new object();
    private HttpListener _listener;
    private CancellationTokenSource _cancellationTokenSource;

    private HttpServer() { }

    public static HttpServer Instance()
    {
        lock (Padlock)
        {
            if (_instance == null)
            {
                _instance = new HttpServer();
            }
            return _instance;
        }
    }

    public void Start(string urlPrefix)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add(urlPrefix);
        _listener.Start();
        Console.WriteLine($"Listening for connections on {urlPrefix}");

        Task.Run(() =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                HttpListenerContext context = _listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/data")
                {
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();
                        Console.WriteLine(json);
                    }

                    string responseString = "Data received and processed.";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
            }
        }, _cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (_listener != null && _listener.IsListening)
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("HTTP server stopped.");
        }
    }
}