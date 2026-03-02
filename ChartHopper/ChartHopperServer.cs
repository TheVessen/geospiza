using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChartHopper;

/// <summary>
///     Singleton HTTP server that serves the embedded SvelteKit app and chart session data.
///     One server instance runs for the lifetime of the host process (Rhino).
///     Each chart/dashboard is identified by a short session ID passed as a query parameter.
/// </summary>
public sealed class ChartHopperServer : IDisposable
{
    private const string ResourcePrefix = "ChartHopper.EmbeddedAssets.web.";

    private static readonly Lazy<ChartHopperServer> _instance =
        new(() => new ChartHopperServer(), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    private static readonly Dictionary<string, string> MimeTypes = new()
    {
        { ".html", "text/html; charset=utf-8" },
        { ".css", "text/css; charset=utf-8" },
        { ".js", "application/javascript; charset=utf-8" },
        { ".json", "application/json; charset=utf-8" },
        { ".svg", "image/svg+xml" },
        { ".ico", "image/x-icon" },
        { ".png", "image/png" },
        { ".woff", "font/woff" },
        { ".woff2", "font/woff2" },
        { ".ttf", "font/ttf" },
        { ".txt", "text/plain; charset=utf-8" }
    };

    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly HashSet<string> _resourceNames;
    private readonly ConcurrentDictionary<string, string> _sessions = new();
    private readonly object _lock = new();

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public static ChartHopperServer Instance => _instance.Value;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }
    public string BaseUrl => $"http://localhost:{Port}";

    private ChartHopperServer()
    {
        _resourceNames = new HashSet<string>(_assembly.GetManifestResourceNames());
        Start();
    }

    /// <summary>
    ///     Register a dashboard config JSON and return the URL to open in the browser.
    /// </summary>
    public string RegisterSession(string dashboardJson)
    {
        var sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _sessions[sessionId] = dashboardJson;
        return $"{BaseUrl}/?session={sessionId}";
    }

    /// <summary>
    ///     Open the given URL in the system default browser.
    /// </summary>
    public static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // Silently fail — URL is still returned to the caller.
        }
    }

    // ── Server lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        lock (_lock)
        {
            if (IsRunning) return;

            Port = FindAvailablePort();
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            IsRunning = true;

            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            try { _listener?.Stop(); _listener?.Close(); } catch { /* ignore */ }
            _listener = null;
            IsRunning = false;
        }
        _cts?.Dispose();
    }

    // ── Request handling ──────────────────────────────────────────────────────

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && IsRunning)
        {
            try
            {
                var ctx = await _listener!.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(ctx, ct), ct);
            }
            catch (HttpListenerException) { break; }
            catch { /* ignore transient errors */ }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;
            var path = req.Url!.AbsolutePath.TrimStart('/');

            // API route: /api/data?session=<id>
            if (path == "api/data")
            {
                var sessionId = req.QueryString["session"];
                if (sessionId != null && _sessions.TryGetValue(sessionId, out var json))
                    await SendJsonAsync(res, json, ct);
                else
                    await SendTextAsync(res, 404, "Session not found", ct);
                return;
            }

            // Static file serving
            // LogicalName format: "ChartHopper.EmbeddedAssets.web." + dot-separated-path
            // e.g. URL "_app/immutable/chunks/foo.js" → "ChartHopper.EmbeddedAssets.web._app.immutable.chunks.foo.js"
            if (string.IsNullOrEmpty(path)) path = "index.html";

            var resourceKey = ResourcePrefix + path.Replace('/', '.').Replace('\\', '.');

            // SPA fallback: non-file routes → index.html
            if (!_resourceNames.Contains(resourceKey) && !path.Contains('.'))
                resourceKey = ResourcePrefix + "index.html";

            if (!_resourceNames.Contains(resourceKey))
            {
                await SendTextAsync(res, 404, "Not found", ct);
                return;
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            res.ContentType = MimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
            res.AddHeader("Cache-Control", "no-cache");

            using var stream = _assembly.GetManifestResourceStream(resourceKey)!;
            res.ContentLength64 = stream.Length;
            res.StatusCode = 200;
            await stream.CopyToAsync(res.OutputStream, 81920, ct);
            res.Close();
        }
        catch
        {
            try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    private static async Task SendJsonAsync(HttpListenerResponse res, string json, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        res.StatusCode = 200;
        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
        res.Close();
    }

    private static async Task SendTextAsync(HttpListenerResponse res, int status, string message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        res.ContentType = "text/plain; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        res.StatusCode = status;
        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
        res.Close();
    }

    // ── Port discovery ────────────────────────────────────────────────────────

    private static int FindAvailablePort()
    {
        var rng = new Random();
        for (var i = 0; i < 20; i++)
        {
            var port = rng.Next(49152, 65535);
            try
            {
                using var probe = new HttpListener();
                probe.Prefixes.Add($"http://localhost:{port}/");
                probe.Start();
                probe.Stop();
                return port;
            }
            catch { /* try next */ }
        }
        throw new Exception("Could not find an available port for ChartHopperServer.");
    }
}
