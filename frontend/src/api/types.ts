// Mirrors LanternAI.Api/Models — keep in sync with the backend records.

export interface ColumnSchema {
  name: string;
  kqlType: string;
  description: string;
}

export interface TableSchema {
  name: string;
  description: string;
  columns: ColumnSchema[];
}

export type FilterOperator = "Equals" | "NotEquals" | "Contains" | "GreaterThan" | "LessThan";
export type AggregationFunction = "Count" | "Sum" | "Avg" | "Min" | "Max";

export interface QueryFilter {
  column: string;
  operator: FilterOperator;
  value: string;
}

export interface QueryTimeRange {
  column: string;
  lookbackHours: number;
}

export interface QueryAggregation {
  function: AggregationFunction;
  column: string | null;
  groupBy: string[] | null;
}

export interface QueryPlan {
  table: string;
  tables?: string[];
  columns: string[] | null;
  filters: QueryFilter[];
  timeRange: QueryTimeRange | null;
  aggregation: QueryAggregation | null;
  limit: number | null;
}

export interface QueryResultData {
  columns: string[];
  rows: Record<string, unknown>[];
}

export interface QueryResponse {
  question: string;
  generatedKql: string;
  plan: QueryPlan;
  result: QueryResultData;
}

export interface ProblemDetails {
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  correlationId?: string;
}

export interface HealthStatus {
  ok: boolean;
}
