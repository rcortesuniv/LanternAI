import { useMutation, useQuery } from "@tanstack/react-query";
import { api } from "./client";

export function useTables() {
  return useQuery({
    queryKey: ["tables"],
    queryFn: api.listTables,
    staleTime: 5 * 60 * 1000,
  });
}

export function useRunQuery() {
  return useMutation({
    mutationFn: (question: string) => api.runQuery(question),
  });
}
