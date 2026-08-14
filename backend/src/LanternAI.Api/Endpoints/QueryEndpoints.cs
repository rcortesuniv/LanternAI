using LanternAI.Api.Infrastructure;
using LanternAI.Api.Models;
using LanternAI.Api.Services.Execution;
using LanternAI.Api.Services.QueryPlanning;

namespace LanternAI.Api.Endpoints;

/// <summary>The core NL -> KQL -> results endpoint.</summary>
public static class QueryEndpoints
{
    private const int MaxQuestionLength = 500;

    public static void MapQueryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/query", async (QueryRequest request, IQueryPlanService planService, IQueryExecutor executor, CancellationToken ct) =>
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

            var plan = await planService.BuildPlanAsync(request.Question, ct);
            var result = executor.Execute(plan);
            var generatedKql = KqlRenderer.Render(plan);

            return Results.Ok(new QueryResponse(request.Question, generatedKql, plan, result));
        })
        .WithName("RunQuery")
        .WithSummary("Translate a natural-language question into a query plan and execute it against the simulated tables.")
        .RequireRateLimiting(RateLimiting.QueryPolicy);
    }

    public sealed record QueryRequest(string Question);
}
