
type ConnectionStatus = "connecting" | "connected" | "timeout" | "error" | "disconnected";


type EventCallback = (...args: any[]) => void;

export class EventEmitter {
  private events: Map<string, EventCallback[]>;

  constructor() {
    this.events = new Map();
  }

  on(event: string, callback: EventCallback): void {
    if (!this.events.has(event)) {
      this.events.set(event, []);
    }
    this.events.get(event)?.push(callback);
  }

  off(event: string, callback: EventCallback): void {
    const callbacks = this.events.get(event);
    if (callbacks) {
      const index = callbacks.indexOf(callback);
      if (index !== -1) {
        callbacks.splice(index, 1);
      }
      if (callbacks.length === 0) {
        this.events.delete(event);
      }
    }
  }

  emit(event: string, ...args: any[]): void {
    const callbacks = this.events.get(event);
    if (callbacks) {
      callbacks.forEach(callback => callback(...args));
    }
  }
}

export class WebSocketService extends EventEmitter {
  private socket: WebSocket | null = null;
  private reconnectTimer: NodeJS.Timeout | null = null;
  private readonly WS_URL: string;
  private readonly RECONNECT_DELAY = 3000;
  private readonly CONNECTION_TIMEOUT = 5000;
  public connectionStatus: ConnectionStatus = "disconnected";
  private isMounted = false;

  constructor(url: string) {
    super();
    this.WS_URL = url;
  }

  public connect(): void {
    this.isMounted = true;
    this.connectWebSocket();
  }

  private connectWebSocket(): void {
    if (!this.isMounted) return;

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    this.connectionStatus = "connecting";
    try {
      this.socket = new WebSocket(this.WS_URL);
    } catch (err) {
      this.connectionStatus = "error";
      this.emit('error', 'Failed to create WebSocket');
      return;
    }

    const timeoutId = setTimeout(() => {
      if (this.socket?.readyState !== WebSocket.OPEN) {
        this.socket?.close();
        this.connectionStatus = "timeout";
        this.emit('error', 'Connection timeout - server not responding');
      }
    }, this.CONNECTION_TIMEOUT);

    this.socket.onopen = () => {
      clearTimeout(timeoutId);
      this.connectionStatus = "connected";
      this.emit('connected');
    };

    this.socket.onmessage = (event: MessageEvent) => {
      if (typeof event.data === "string" && event.data.trim()[0] !== "{") {
        console.warn("Non-JSON message received:", event.data);
        return;
      }

      try {
        const data = JSON.parse(event.data);
        this.emit('message', data);
      } catch (err) {
        console.error("Error parsing message:", err);
      }
    };

    this.socket.onerror = (err) => {
      clearTimeout(timeoutId);
      this.connectionStatus = "error";
      this.emit('error', err);
    };

    this.socket.onclose = () => {
      clearTimeout(timeoutId);
      this.connectionStatus = "disconnected";
      this.emit('disconnected');
      if (this.isMounted) {
        this.reconnectTimer = setTimeout(() => this.connectWebSocket(), this.RECONNECT_DELAY);
      }
    };
  }

  public send(data: any): void {
    if (this.socket?.readyState === WebSocket.OPEN) {
      try {
        this.socket.send(JSON.stringify(data));
      } catch (err) {
        this.emit('error', 'Error sending message');
      }
    } else {
      this.emit('error', 'WebSocket is not open');
    }
  }

  public runSolver(): void {
    this.send({ command: "run" });
  }

  public disconnect(): void {
    this.isMounted = false;
    if (this.socket) this.socket.close();
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
  }
}