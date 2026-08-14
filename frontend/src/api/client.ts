import type { ProblemDetails, QueryResponse, TableSchema } from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5020";

/** Error carrying the backend's ProblemDetails so the UI can show a specific, non-leaky message. */
export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });

  if (!response.ok) {
    const problem: ProblemDetails | null = await response.json().catch(() => null);
    const message =
      problem?.errors && Object.values(problem.errors).flat()[0]
        ? Object.values(problem.errors).flat()[0]
        : (problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}.`);
    throw new ApiError(message, response.status);
  }

  return (await response.json()) as T;
}

export const api = {
  listTables: () => request<TableSchema[]>("/api/tables"),
  runQuery: (question: string) =>
    request<QueryResponse>("/api/query", {
      method: "POST",
      body: JSON.stringify({ question }),
    }),
};
