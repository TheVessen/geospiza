using System;
using Fleck;
using Rhino;

namespace GeospizaPlugin.AsyncComponent;

/// <summary>
///     A helper class that encapsulates the WebSocket server functionality.
/// </summary>
public class WebSocketService : IDisposable
{
    private readonly string _endpoint;
    private readonly object _lock = new();
    private IWebSocketConnection _currentSocket;
    private WebSocketServer _server;

    public WebSocketService(string endpoint)
    {
        _endpoint = endpoint;
    }

    public bool IsRunning { get; private set; }

    public void Dispose()
    {
        Stop();
    }

    public event Action<string> OnMessageReceived;
    public event Action OnClientConnected;
    public event Action OnClientDisconnected;

    public void Start()
    {
        lock (_lock)
        {
            if (IsRunning)
                return;

            _server = new WebSocketServer(_endpoint)
            {
                RestartAfterListenError = true
            };

            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    lock (_lock)
                    {
                        _currentSocket = socket;
                    }

                    OnClientConnected?.Invoke();
                    socket.Send("Connected to server");
                };

                socket.OnClose = () =>
                {
                    lock (_lock)
                    {
                        if (_currentSocket == socket)
                            _currentSocket = null;
                    }

                    OnClientDisconnected?.Invoke();
                };

                socket.OnError = exception => { RhinoApp.WriteLine($"WebSocket error: {exception.Message}"); };

                socket.OnMessage = message => { OnMessageReceived?.Invoke(message); };
            });

            IsRunning = true;
        }
    }

    public void SendMessage(string message)
    {
        lock (_lock)
        {
            if (_currentSocket != null && _currentSocket.IsAvailable)
                _currentSocket.Send(message);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            try
            {
                _currentSocket?.Close();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error closing socket: {ex.Message}");
            }

            try
            {
                _server?.Dispose();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error disposing server: {ex.Message}");
            }

            _currentSocket = null;
            _server = null;
            IsRunning = false;
        }
    }
}