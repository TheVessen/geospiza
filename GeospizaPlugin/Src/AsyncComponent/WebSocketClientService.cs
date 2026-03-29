using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rhino;

namespace GeospizaPlugin.AsyncComponent;

/// <summary>
///     WebSocket client that connects to an external server.
///     Used in headless/RhinoCompute mode where GH cannot host a server.
/// </summary>
public class WebSocketClientService : IDisposable
{
    private readonly string _endpoint;
    private readonly object _lock = new();
    private ClientWebSocket _socket;
    private CancellationTokenSource _cts;

    public WebSocketClientService(string endpoint)
    {
        _endpoint = endpoint;
    }

    public bool IsConnected { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnMessageReceived;

    public async Task ConnectAsync()
    {
        lock (_lock)
        {
            if (IsConnected) return;
            _cts = new CancellationTokenSource();
            _socket = new ClientWebSocket();
        }

        try
        {
            await _socket.ConnectAsync(new Uri(_endpoint), _cts.Token);
            IsConnected = true;
            OnConnected?.Invoke();
            _ = Task.Run(ReceiveLoop);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"WebSocketClient: connect failed — {ex.Message}");
            IsConnected = false;
        }
    }

    public void SendMessage(string message)
    {
        if (!IsConnected || _socket?.State != WebSocketState.Open)
            return;

        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);

        try
        {
            // Fire-and-forget send — we're on the GH thread, don't block
            _socket.SendAsync(segment, WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"WebSocketClient: send failed — {ex.Message}");
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];
        try
        {
            while (_socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"WebSocketClient: receive error — {ex.Message}");
        }
        finally
        {
            IsConnected = false;
            OnDisconnected?.Invoke();
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            _cts?.Cancel();
            try
            {
                if (_socket?.State == WebSocketState.Open)
                    _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None)
                        .GetAwaiter().GetResult();
            }
            catch { }

            _socket?.Dispose();
            _socket = null;
            IsConnected = false;
        }
    }

    public void Dispose() => Disconnect();
}
