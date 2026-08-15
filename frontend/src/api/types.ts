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
  rowCount?: number;
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
  usage?: QueryUsage | null;
  diagnostics?: QueryDiagnostics | null;
  explanation?: QueryExplanation | null;
  metrics?: QueryMetrics | null;
  auditId?: string | null;
  resultSummary?: string | null;
}

export interface QueryUsage {
  promptTokens?: number | null;
  completionTokens?: number | null;
  totalTokens?: number | null;
}

export interface QueryDiagnostics {
  cacheHit: boolean;
  cacheKeyVersion: string;
  costTier: string;
  estimatedRowsScanned: number;
  estimatedWorkUnits: number;
  costExplanation: string;
}

export interface QueryExplanation {
  summary: string;
  reasons: string[];
  confidence: string;
  warnings: string[];
  unresolvedAmbiguities: string[];
}

export interface QueryMetrics {
  costTier: string;
  estimatedRowsScanned: number;
  estimatedWorkUnits: number;
  resultRowCount: number;
  promptTokens: number;
  completionTokens: number;
  durationMs: number;
  cacheHit: boolean;
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

export interface SystemCapabilities {
  authentication: { configured: boolean; provider: string };
  data: { configured: boolean; provider: string };
  languageModel: { provider: string; model: string };
  sourceCount: number;
  dataSources: Array<{ name: string; kind: string; supportsJoins: boolean; supportsAggregations: boolean; supportsCaching: boolean }>;
}

/** Request payload for POST /api/query. */
export interface QueryRequestPayload {
  question: string;
  timeRangeHours?: number | null;
  summarize?: boolean;
  previousQuestion?: string | null;
  previousPlan?: QueryPlan | null;
  previousSummary?: string | null;
}
