import type { QueryResponse } from "../../api/types";

export interface ChatTurn {
  id: string;
  question: string;
  status: "loading" | "success" | "error";
  response?: QueryResponse;
  errorMessage?: string;
}
