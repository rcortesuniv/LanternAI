import { useMutation, useQuery } from "@tanstack/react-query";
import { api } from "./client";

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
    retry: 1,
    staleTime: 10_000,
  });
}

export function useRunQuery() {
  return useMutation({
    mutationFn: (question: string) => api.runQuery(question),
  });
}
