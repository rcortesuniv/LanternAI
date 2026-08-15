namespace LanternAI.Api.Services.QueryPlanning;

/// <summary>
/// Thrown when the LLM's response can't be parsed into a <see cref="Models.QueryPlan"/>,
/// or the plan references a table/column that doesn't exist in the catalog.
/// Mapped to a 400 response with this message only — never the raw LLM output
/// or an internal stack trace.
/// </summary>
public sealed class InvalidQueryPlanException(string message) : Exception(message);
