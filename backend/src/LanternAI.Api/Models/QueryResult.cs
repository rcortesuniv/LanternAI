namespace LanternAI.Api.Models;

/// <summary>Tabular result of executing a <see cref="QueryPlan"/> against the (simulated) data source.</summary>
public sealed record QueryResult(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

/// <summary>Full response returned by POST /api/query: the answer plus the transparency trail behind it.</summary>
public sealed record QueryResponse(string Question, string GeneratedKql, QueryPlan Plan, QueryResult Result, QueryUsage? Usage = null, QueryDiagnostics? Diagnostics = null);

public sealed record QueryUsage(int? PromptTokens, int? CompletionTokens, int? TotalTokens);

public sealed record QueryDiagnostics(bool CacheHit, string CacheKeyVersion, string CostTier, int EstimatedRowsScanned, double EstimatedWorkUnits, string CostExplanation);
