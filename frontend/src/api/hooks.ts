import { useMutation, useQuery } from "@tanstack/react-query";
import { api } from "./client";
import type { QueryRequestPayload } from "./types";

export function useTables() {
  return useQuery({
    queryKey: ["tables"],
    queryFn: api.listTables,
    staleTime: 5 * 60 * 1000,
  });
}

export function useBackendHealth() {
  return useQuery({
    queryKey: ["backend-health"],
    queryFn: api.checkHealth,
    refetchInterval: 30_000,
    refetchIntervalInBackground: true,
    refetchOnWindowFocus: true,
    retry: 3,
    retryDelay: 1_000,
    staleTime: 10_000,
  });
}

export function useModelReadiness() {
  return useQuery({
    queryKey: ["model-readiness"],
    queryFn: api.checkReadiness,
    refetchInterval: 30_000,
    refetchIntervalInBackground: true,
    refetchOnWindowFocus: true,
    retry: 1,
    retryDelay: 1_000,
    staleTime: 10_000,
  });
}

export function useCapabilities() {
  return useQuery({
    queryKey: ["capabilities"],
    queryFn: api.getCapabilities,
    refetchInterval: 60_000,
    retry: 2,
    staleTime: 30_000,
  });
}

export function useRunQuery() {
  return useMutation({
    mutationFn: (payload: QueryRequestPayload) => api.runQuery(payload),
  });
}

import type { SessionQuery, QueryPlan, QueryResultData } from "./types";
import { analysisApi } from "./client";

export function usePromptbooks() {
  return useQuery({
    queryKey: ["promptbooks"],
    queryFn: analysisApi.listPromptbooks,
    staleTime: 5 * 60 * 1000,
  });
}

export function useExecutePromptbook() {
  return useMutation({
    mutationFn: (id: string) => analysisApi.executePromptbook(id),
  });
}

export function useDetectAnomalies() {
  return useMutation({
    mutationFn: ({ plan, result }: { plan: QueryPlan; result: QueryResultData }) =>
      analysisApi.detectAnomalies(plan, result),
  });
}

export function useIncidentSummary() {
  return useMutation({
    mutationFn: (queries: SessionQuery[]) => analysisApi.generateIncidentSummary(queries),
  });
}
