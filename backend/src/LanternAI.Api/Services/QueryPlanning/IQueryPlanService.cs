using LanternAI.Api.Models;

namespace LanternAI.Api.Services.QueryPlanning;

/// <summary>Turns a natural-language question into a validated <see cref="QueryPlan"/>.</summary>
public interface IQueryPlanService
{
    Task<QueryPlan> BuildPlanAsync(string question, CancellationToken cancellationToken = default);
    Task<QueryPlanBuildResult> BuildPlanWithUsageAsync(string question, CancellationToken cancellationToken = default);
}

public sealed record QueryPlanBuildResult(QueryPlan Plan, int? PromptTokens, int? CompletionTokens, int? TotalTokens);
