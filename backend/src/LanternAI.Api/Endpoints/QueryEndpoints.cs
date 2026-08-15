using LanternAI.Api.Infrastructure;
using LanternAI.Api.Models;
using LanternAI.Api.Services.Execution;
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
        var endpoint = app.MapPost("/api/query", async (QueryRequest request, IQueryPlanService planService, IQueryExecutor executor, QueryCostEstimator costEstimator, IAuditStore auditStore, IMemoryCache cache, ILoggerFactory loggerFactory, HttpContext httpContext, CancellationToken ct) =>
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
            var cacheKey = $"query:v1:{tenant.TenantId}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Question.Trim().ToLowerInvariant())))}";
            if (cache.TryGetValue(cacheKey, out QueryResponse? cachedResponse) && cachedResponse is not null)
            {
                return Results.Ok(cachedResponse with { Diagnostics = cachedResponse.Diagnostics is { } diagnostics ? diagnostics with { CacheHit = true } : null });
            }

            var logger = loggerFactory.CreateLogger("LanternAI.QueryAudit");
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation("Query started with correlation id {CorrelationId}", httpContext.TraceIdentifier);
            var planned = await planService.BuildPlanWithUsageAsync(request.Question, ct);
            var plan = planned.Plan;
            var result = executor.Execute(plan);
            var generatedKql = KqlRenderer.Render(plan);
            stopwatch.Stop();
            logger.LogInformation("Query completed in {DurationMs} ms across {SourceCount} source(s), returning {RowCount} row(s)", stopwatch.Elapsed.TotalMilliseconds, plan.Tables?.Count ?? 1, result.Rows.Count);
            auditStore.Append(new AuditEvent(DateTimeOffset.UtcNow, "query.completed", httpContext.TraceIdentifier, tenant.TenantId, tenant.SubjectId, request.Question, result.Rows.Count, stopwatch.Elapsed.TotalMilliseconds));

            var cost = costEstimator.Estimate(plan);
            var response = new QueryResponse(request.Question, generatedKql, plan, result, new QueryUsage(planned.PromptTokens, planned.CompletionTokens, planned.TotalTokens), new QueryDiagnostics(false, "v1", cost.Tier, cost.EstimatedRowsScanned, cost.EstimatedWorkUnits, cost.Explanation));
            cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
            return Results.Ok(response);
        })
        .WithName("RunQuery")
        .WithSummary("Translate a natural-language question into a query plan and execute it against the simulated tables.")
        .RequireRateLimiting(RateLimiting.QueryPolicy);
        if (requireAuthorization) endpoint.RequireAuthorization("Lantern.User");
    }

    public sealed record QueryRequest(string Question);
}
