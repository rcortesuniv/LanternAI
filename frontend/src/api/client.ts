import type { HealthStatus, ProblemDetails, QueryResponse, SystemCapabilities, TableSchema } from "./types";

/**
 * In GitHub Codespaces (and VS Code's forwarded-port URLs generally), the
 * page is served from a host like
 * `https://<codespace-name>-5173.app.github.dev` — "localhost" in that
 * browser tab means the visitor's own machine, not the Codespace, so a
 * hardcoded `localhost:5020` default would silently fail there. When no
 * explicit VITE_API_BASE_URL is set, detect that pattern and swap in the
 * backend's port to derive its forwarded URL automatically.
 */
function resolveApiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL;
  if (configured) return configured;

  const { hostname, protocol } = window.location;
  const forwardedPortMatch = hostname.match(/^(.+)-\d+\.(app\.github\.dev|github\.dev)$/);
  if (forwardedPortMatch) {
    const [, prefix, domain] = forwardedPortMatch;
    return `${protocol}//${prefix}-5020.${domain}`;
  }

  return "http://localhost:5020";
}

const BASE_URL = resolveApiBaseUrl();

/** Error carrying the backend's ProblemDetails so the UI can show a specific, non-leaky message. */
export class ApiError extends Error {
  status: number;
  correlationId?: string;

  constructor(message: string, status: number, correlationId?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.correlationId = correlationId;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), 75_000);
  const correlationId = crypto.randomUUID();

  try {
    const response = await fetch(`${BASE_URL}${path}`, {
      ...init,
      signal: init?.signal ?? controller.signal,
      headers: { "Content-Type": "application/json", "X-Correlation-ID": correlationId, ...init?.headers },
    });

    if (!response.ok) {
      const problem: ProblemDetails | null = await response.json().catch(() => null);
      const message =
        problem?.errors && Object.values(problem.errors).flat()[0]
          ? Object.values(problem.errors).flat()[0]
          : (problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}.`);
      throw new ApiError(message, response.status, problem?.correlationId ?? response.headers.get("X-Correlation-ID") ?? undefined);
    }

    const contentType = response.headers.get("content-type") ?? "";
    if (!contentType.includes("application/json")) return {} as T;
    return (await response.json()) as T;
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw new ApiError("The request timed out. The local model may still be warming up.", 408, correlationId);
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}

export const api = {
  listTables: () => request<TableSchema[]>("/api/tables"),
  checkHealth: () => request<HealthStatus>("/health/live").then(() => ({ ok: true })),
  checkReadiness: () => request<HealthStatus>("/health/ready").then(() => ({ ok: true })),
  getCapabilities: () => request<SystemCapabilities>("/api/capabilities"),
  runQuery: (question: string) =>
    request<QueryResponse>("/api/query", {
      method: "POST",
      body: JSON.stringify({ question }),
    }),
};
