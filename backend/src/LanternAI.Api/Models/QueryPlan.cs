namespace LanternAI.Api.Models;

/// <summary>
/// A structured, validated representation of "what the user wants to know",
/// produced by the LLM as JSON and checked against the table catalog before
/// anything is executed. This is the safety boundary: we never run raw
/// LLM-generated KQL text against data — we execute this typed plan instead,
/// and only render KQL text from it for display/transparency. See
/// docs/SECURITY.md for the rationale.
/// </summary>
public sealed record QueryPlan
{
    public required string Table { get; init; }

    /// <summary>Columns to project. Empty/null means "all columns".</summary>
    public IReadOnlyList<string>? Columns { get; init; }

    public IReadOnlyList<QueryFilter> Filters { get; init; } = [];

    public QueryTimeRange? TimeRange { get; init; }

    public QueryAggregation? Aggregation { get; init; }

    /// <summary>Optional cap on returned rows, applied after filtering/aggregation.</summary>
    public int? Limit { get; init; }
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    Contains,
    GreaterThan,
    LessThan,
}

public sealed record QueryFilter(string Column, FilterOperator Operator, string Value);

/// <summary>Relative lookback window, e.g. "last 24 hours" -> Hours = 24.</summary>
public sealed record QueryTimeRange(string Column, double LookbackHours);

public enum AggregationFunction
{
    Count,
    Sum,
    Avg,
    Min,
    Max,
}

public sealed record QueryAggregation(AggregationFunction Function, string? Column, IReadOnlyList<string>? GroupBy);
