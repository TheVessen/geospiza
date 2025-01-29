export enum ConnectionStatus {
  DISCONNECTED = "disconnected",
  CONNECTING = "connecting",
  CONNECTED = "connected",
  ERROR = "error",
  TIMEOUT = "timeout"
}

export interface WebSocketMessage {
  command: "run" | "status" | "ping" | "pong";
  data?: unknown;
}

export interface ServerResponse {
  type: "response";
  status: string;
  data?: unknown;
}