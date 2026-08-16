using LanternAI.Api.Infrastructure;
using LanternAI.Api.Models;
using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Services.Execution;
using LanternAI.Api.Services.Llm;
using LanternAI.Api.Services.QueryPlanning;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace LanternAI.Api.Endpoints;

/// <summary>The core NL -> KQL -> results endpoint.</summary>
public static class QueryEndpoints
{
    private const int MaxQuestionLength = 500;

    public static void MapQueryEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization = false)
    {
        var endpoint = app.MapPost("/api/query", async (
            QueryRequest request,
            IQueryPlanService planService,
            IQueryExecutor executor,
            IEventTableCatalog catalog,
            QueryCostEstimator costEstimator,
            IAuditStore auditStore,
            IMemoryCache cache,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["question"] = ["Question is required."],
                });
            }

            if (request.Question.Length > MaxQuestionLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["question"] = [$"Question must be {MaxQuestionLength} characters or fewer."],
                });
            }

            var tenant = httpContext.User.GetTenantContext() ?? new TenantContext("local", "anonymous");
            var cacheKey = $"query:v2:{tenant.TenantId}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Question.Trim().ToLowerInvariant())))}:{request.TimeRangeHours ?? 0}:{request.Summarize}";
            if (cache.TryGetValue(cacheKey, out QueryResponse? cachedResponse) && cachedResponse is not null)
            {
                return Results.Ok(cachedResponse with
                {
                    Diagnostics = cachedResponse.Diagnostics is { } diagnostics ? diagnostics with { CacheHit = true } : null,
                    Metrics = cachedResponse.Metrics is { } cachedMetrics ? cachedMetrics with { CacheHit = true } : null,
                });
            }

            var logger = loggerFactory.CreateLogger("LanternAI.QueryAudit");
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation("Query started with correlation id {CorrelationId}", httpContext.TraceIdentifier);

            // Build conversation context for follow-up questions.
            ConversationContext? context = null;
            if (!string.IsNullOrWhiteSpace(request.PreviousQuestion))
            {
                context = new ConversationContext(
                    request.PreviousQuestion,
                    request.PreviousPlan,
                    request.PreviousSummary);
            }

            var planned = await planService.BuildPlanWithUsageAsync(request.Question, context, ct);
            var plan = planned.Plan;

            // Apply explicit time range override from the time range picker.
            if (request.TimeRangeHours is { } hours && hours > 0)
            {
                if (plan.TimeRange is { } existing)
                {
                    plan = plan with { TimeRange = existing with { LookbackHours = hours } };
                }
                else
                {
                    // LLM didn't include a time range — find the first datetime column on the primary table.
                    var table = catalog.GetTable(plan.Table);
                    var datetimeColumn = table?.Columns.FirstOrDefault(c =>
                        c.KqlType.Equals("datetime", StringComparison.OrdinalIgnoreCase));
                    if (datetimeColumn is not null)
                    {
                        plan = plan with { TimeRange = new QueryTimeRange(datetimeColumn.Name, hours) };
                    }
                }
            }

            var result = executor.Execute(plan);
            var generatedKql = KqlRenderer.Render(plan);

            // Generate a natural-language result summary if requested.
            string? resultSummary = null;
            if (request.Summarize)
            {
                try
                {
                    resultSummary = await planService.SummarizeAsync(request.Question, plan, result, ct);
                }
                catch (Exception ex) when (ex is LlmUnavailableException)
                {
                    logger.LogWarning(ex, "Result summary generation failed for correlation id {CorrelationId}", httpContext.TraceIdentifier);
                }
            }

            stopwatch.Stop();
            logger.LogInformation("Query completed in {DurationMs} ms across {SourceCount} source(s), returning {RowCount} row(s)", stopwatch.Elapsed.TotalMilliseconds, plan.Tables?.Count ?? 1, result.Rows.Count);
            var auditId = Guid.NewGuid().ToString("N");
            auditStore.Append(new AuditEvent(DateTimeOffset.UtcNow, "query.completed", httpContext.TraceIdentifier, tenant.TenantId, tenant.SubjectId, request.Question, result.Rows.Count, stopwatch.Elapsed.TotalMilliseconds));

            var cost = costEstimator.Estimate(plan);
            var explanation = BuildExplanation(plan, request.Question);
            var metrics = new QueryMetrics(cost.Tier, cost.EstimatedRowsScanned, cost.EstimatedWorkUnits, result.Rows.Count, planned.PromptTokens ?? 0, planned.CompletionTokens ?? 0, stopwatch.Elapsed.TotalMilliseconds, false);
            var response = new QueryResponse(
                request.Question,
                generatedKql,
                plan,
                result,
                new QueryUsage(planned.PromptTokens, planned.CompletionTokens, planned.TotalTokens),
                new QueryDiagnostics(false, "v1", cost.Tier, cost.EstimatedRowsScanned, cost.EstimatedWorkUnits, cost.Explanation),
                explanation,
                metrics,
                auditId,
                resultSummary);
            cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
            return Results.Ok(response);
        })
        .WithName("RunQuery")
        .WithSummary("Translate a natural-language question into a query plan and execute it against the simulated tables.")
        .RequireRateLimiting(RateLimiting.QueryPolicy);
        if (requireAuthorization) endpoint.RequireAuthorization("Lantern.User");
    }

    private static QueryExplanation BuildExplanation(QueryPlan plan, string question)
    {
        var reasons = new List<string>();
        reasons.Add($"Selected '{plan.Table}' as the primary source for this question.");

        if (plan.Tables is { Count: > 0 })
        {
            reasons.Add($"Combined the requested sources: {string.Join(", ", plan.Tables)}.");
        }

        if (plan.TimeRange is not null)
        {
            reasons.Add($"Applied a {plan.TimeRange.LookbackHours}-hour lookback on '{plan.TimeRange.Column}'.");
        }

        if (plan.Filters.Count > 0)
        {
            reasons.Add($"Narrowed the result set with {plan.Filters.Count} filter(s) against the selected schema.");
        }

        if (plan.Aggregation is not null)
        {
            var target = plan.Aggregation.Function == AggregationFunction.Count ? "all rows" : (plan.Aggregation.Column ?? "the selected numeric column");
            reasons.Add($"Summarized the data using {plan.Aggregation.Function} on {target}.");
        }

        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(question))
        {
            warnings.Add("The question was empty, so the plan used the default table selection fallback.");
        }

        return new QueryExplanation(
            $"This plan was generated to answer: \"{question.Trim()}\".",
            reasons,
            plan.Aggregation is not null ? "medium" : "high",
            warnings,
            []);
    }

    public sealed record QueryRequest(
        string Question,
        double? TimeRangeHours = null,
        bool Summarize = false,
        string? PreviousQuestion = null,
        QueryPlan? PreviousPlan = null,
        string? PreviousSummary = null);
}
