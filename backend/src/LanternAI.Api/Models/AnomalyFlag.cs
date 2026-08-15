namespace LanternAI.Api.Models;

/// <summary>A flagged pattern or anomaly found in query results.</summary>
public sealed record AnomalyFlag(
    string Severity,       // "critical", "warning", "info"
    string Title,
    string Description,
    IReadOnlyList<string> Evidence);  // specific row data that triggered the flag

/// <summary>Container for anomaly flags detected on a query result.</summary>
public sealed record AnomalyReport(IReadOnlyList<AnomalyFlag> Flags, bool HasFindings);

/// <summary>Incident summary generated from a session of queries.</summary>
public sealed record IncidentSummary(
    string Title,
    string Overview,
    IReadOnlyList<string> KeyFindings,
    string RiskAssessment,
    IReadOnlyList<string> RecommendedActions,
    int QueryCount,
    int TotalRowsAnalyzed);
