<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import type { ServerResponse } from "./types";

  let messages: (string | ServerResponse)[] = $state([]);
  let socket: WebSocket;
  let connectionStatus = $state("disconnected");
  let reconnectTimer: NodeJS.Timeout;
  let heartbeatTimer: NodeJS.Timeout;
  const WS_URL = "ws://127.0.0.1:8181";
  const RECONNECT_DELAY = 3000;
  const CONNECTION_TIMEOUT = 5000;
  const HEARTBEAT_INTERVAL = 30000; // 30 seconds
  let lastPongTime = 0;

  function checkConnection() {
    const now = Date.now();
    if (now - lastPongTime > HEARTBEAT_INTERVAL * 2) {
      socket?.close();
    }
  }

  function startHeartbeat() {
    heartbeatTimer = setInterval(() => {
      if (socket?.readyState === WebSocket.OPEN) {
        try {
          socket.send(JSON.stringify({ command: "ping" }));
          checkConnection();
        } catch (err) {
          socket.close();
        }
      }
    }, HEARTBEAT_INTERVAL);
  }

  function connectWebSocket() {
    try {
      connectionStatus = "connecting";
      socket = new WebSocket(WS_URL);
      lastPongTime = Date.now(); // Reset pong timer

      const timeoutId = setTimeout(() => {
        if (socket.readyState !== WebSocket.OPEN) {
          socket.close();
          connectionStatus = "timeout";
          messages = [
            ...messages,
            "Connection timeout - server not responding",
          ];
        }
      }, CONNECTION_TIMEOUT);

      socket.onopen = () => {
        clearTimeout(timeoutId);
        console.log("Connected to Grasshopper WebSocket");
        connectionStatus = "connected";
        messages = [...messages, "Connected to server"];
        startHeartbeat();
      };

      socket.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          if (data.command === "pong") {
            lastPongTime = Date.now();
            return;
          }
          messages = [
            ...messages,
            typeof data === "object" ? JSON.stringify(data) : data,
          ];
        } catch {
          messages = [...messages, event.data];
        }
      };

      socket.onerror = (err) => {
        clearTimeout(timeoutId);
        console.error("WebSocket error:", err);
        connectionStatus = "error";
        messages = [...messages, "Connection error - server may be offline"];
      };

      socket.onclose = () => {
        clearTimeout(timeoutId);
        clearInterval(heartbeatTimer);
        connectionStatus = "disconnected";
        messages = [...messages, "Connection closed"];
        reconnectTimer = setTimeout(connectWebSocket, RECONNECT_DELAY);
      };
    } catch (err) {
      connectionStatus = "error";
      console.error("Failed to create WebSocket:", err);
    }
  }

  onMount(() => {
    connectWebSocket();
  });

  onDestroy(() => {
    if (socket) socket.close();
    if (reconnectTimer) clearTimeout(reconnectTimer);
    if (heartbeatTimer) clearInterval(heartbeatTimer);
  });

  function runSolver() {
    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ command: "run" }));
    } else {
      messages = [...messages, "WebSocket not connected"];
    }
  }

  function getStatus() {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ command: "status" }));
    } else {
      messages = [...messages, "Connection not open"];
    }
  }
</script>

<main>
  <h1>Remote Grasshopper Solver</h1>

  <div class="status {connectionStatus}">
    Status: {connectionStatus}
  </div>

  <div class="controls">
    <button onclick={runSolver} disabled={connectionStatus !== "connected"}>
      Run Solver
    </button>
    <button onclick={getStatus} disabled={connectionStatus !== "connected"}>
      Get Status
    </button>
  </div>

  <h3>Messages</h3>
  <ul class="messages">
    {#each messages as msg}
      <li>{msg}</li>
    {/each}
  </ul>
</main>

<style>
  .status {
    padding: 8px;
    margin: 8px 0;
    border-radius: 4px;
  }
  .status.connected {
    background: #90ee90;
  }
  .status.connecting {
    background: #ffa500;
  }
  .status.disconnected {
    background: #ffb6c1;
  }
  .status.error {
    background: #ff6b6b;
  }

  .controls {
    margin: 16px 0;
  }

  .messages {
    max-height: 300px;
    overflow-y: auto;
    border: 1px solid #ccc;
    padding: 8px;
  }

  button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
</style>
