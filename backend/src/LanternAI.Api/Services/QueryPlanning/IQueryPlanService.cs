using LanternAI.Api.Models;

namespace LanternAI.Api.Services.QueryPlanning;

/// <summary>Turns a natural-language question into a validated <see cref="QueryPlan"/>.</summary>
public interface IQueryPlanService
{
    Task<QueryPlan> BuildPlanAsync(string question, CancellationToken cancellationToken = default);
}
