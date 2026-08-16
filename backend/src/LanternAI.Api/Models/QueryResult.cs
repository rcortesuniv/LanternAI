namespace LanternAI.Api.Models;

/// <summary>Tabular result of executing a <see cref="QueryPlan"/> against the (simulated) data source.</summary>
public sealed record QueryResult(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

/// <summary>Full response returned by POST /api/query: the answer plus the transparency trail behind it.</summary>
public sealed record QueryResponse(
    string Question,
    string GeneratedKql,
    QueryPlan Plan,
    QueryResult Result,
    QueryUsage? Usage = null,
    QueryDiagnostics? Diagnostics = null,
    QueryExplanation? Explanation = null,
    QueryMetrics? Metrics = null,
    string? AuditId = null,
    string? ResultSummary = null);

public sealed record QueryUsage(int? PromptTokens, int? CompletionTokens, int? TotalTokens);

public sealed record QueryDiagnostics(bool CacheHit, string CacheKeyVersion, string CostTier, int EstimatedRowsScanned, double EstimatedWorkUnits, string CostExplanation);

public sealed record QueryExplanation(
    string Summary,
    IReadOnlyList<string> Reasons,
    string Confidence,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> UnresolvedAmbiguities);

public sealed record QueryMetrics(
    string CostTier,
    int EstimatedRowsScanned,
    double EstimatedWorkUnits,
    int ResultRowCount,
    int PromptTokens,
    int CompletionTokens,
    double DurationMs,
    bool CacheHit = false);

/// <summary>Context from the previous turn, enabling follow-up/refinement questions.</summary>
public sealed record ConversationContext(
    string PreviousQuestion,
    QueryPlan? PreviousPlan = null,
    string? PreviousSummary = null);
