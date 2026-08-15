using LanternAI.Api.Models;

namespace LanternAI.Api.Services.Execution;

/// <summary>
/// Executes a validated <see cref="QueryPlan"/> and returns tabular results.
/// Phase 1's implementation runs against the in-memory mock catalog; a
/// future implementation can satisfy the same interface by sending the
/// equivalent KQL to the real ADX SDK, without any caller needing to change.
/// </summary>
public interface IQueryExecutor
{
    QueryResult Execute(QueryPlan plan);
}
