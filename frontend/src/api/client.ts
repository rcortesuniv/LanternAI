import type { HealthStatus, ProblemDetails, QueryResponse, QueryRequestPayload, SystemCapabilities, TableSchema, PromptbookSummary, PromptbookExecutionResult, AnomalyReport, IncidentSummary, SessionQuery, QueryPlan, QueryResultData } from "./types";

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

/**
 * Request timeout — aligned with the backend's Ollama:TimeoutSeconds (120s)
 * plus a small buffer. Cloud LLM inference can take 10–20s on a cold model,
 * so the old 75s ceiling could prematurely abort legitimate queries.
 * Override via VITE_API_TIMEOUT_MS if needed.
 */
const REQUEST_TIMEOUT_MS = Number(import.meta.env.VITE_API_TIMEOUT_MS) || 130_000;

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

async function request<T>(path: string, init?: RequestInit, timeoutMs?: number): Promise<T> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs ?? REQUEST_TIMEOUT_MS);
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
      throw new ApiError("The request timed out. The model may still be processing — try again in a moment.", 408, correlationId);
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}

export const api = {
  listTables: () => request<TableSchema[]>("/api/tables"),
  /** Liveness — verifies the process is running and can serve HTTP. */
  checkHealth: () => request<HealthStatus>("/health/live").then(() => ({ ok: true })),
  /** Readiness — verifies the Ollama endpoint is reachable and the model is available. */
  checkReadiness: () => request<HealthStatus>("/health/ready").then(() => ({ ok: true })),
  getCapabilities: () => request<SystemCapabilities>("/api/capabilities"),
  runQuery: (payload: QueryRequestPayload) =>
    request<QueryResponse>("/api/query", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
};

export const analysisApi = {
  listPromptbooks: () => request<PromptbookSummary[]>("/api/promptbooks"),
  executePromptbook: (id: string) =>
    request<PromptbookExecutionResult>(`/api/promptbooks/${id}/execute`, { method: "POST" }, 300_000),
  detectAnomalies: (plan: QueryPlan, result: QueryResultData) =>
    request<AnomalyReport>("/api/analyze/anomalies", {
      method: "POST",
      body: JSON.stringify({ plan, result }),
    }),
  generateIncidentSummary: (queries: SessionQuery[]) =>
    request<IncidentSummary>("/api/analyze/incident-summary", {
      method: "POST",
      body: JSON.stringify({ queries }),
    }, 180_000),
};
