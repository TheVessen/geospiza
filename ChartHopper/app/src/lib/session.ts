import type { DashboardConfig } from "./types";

/** Read the session ID from the URL query string. */
export function getSessionId(): string | null {
  if (typeof window === "undefined") return null;
  return new URLSearchParams(window.location.search).get("session");
}

/** Fetch chart data for a session from the C# HTTP server. */
export async function fetchSession(sessionId: string): Promise<DashboardConfig> {
  const res = await fetch(`/api/data?session=${encodeURIComponent(sessionId)}`);
  if (!res.ok) throw new Error(`Server returned ${res.status}`);
  return res.json() as Promise<DashboardConfig>;
}
