using LanternAI.Api.Models;

namespace LanternAI.Api.Services.QueryPlanning;

/// <summary>Turns a natural-language question into a validated <see cref="QueryPlan"/>.</summary>
public interface IQueryPlanService
{
    Task<QueryPlan> BuildPlanAsync(string question, CancellationToken cancellationToken = default);
    Task<QueryPlanBuildResult> BuildPlanWithUsageAsync(string question, CancellationToken cancellationToken = default);

    /// <summary>Build a plan with optional conversation context for follow-up questions.</summary>
    Task<QueryPlanBuildResult> BuildPlanWithUsageAsync(string question, ConversationContext? context, CancellationToken cancellationToken = default);

    /// <summary>Generate a natural-language summary of query results for an analyst.</summary>
    Task<string> SummarizeAsync(string question, QueryPlan plan, QueryResult result, CancellationToken cancellationToken = default);
}

public sealed record QueryPlanBuildResult(QueryPlan Plan, int? PromptTokens, int? CompletionTokens, int? TotalTokens);
