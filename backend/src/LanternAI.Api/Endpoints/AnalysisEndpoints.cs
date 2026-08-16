using LanternAI.Api.Models;
using LanternAI.Api.Services.Analysis;
using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Services.Execution;
using LanternAI.Api.Services.Llm;
using LanternAI.Api.Services.QueryPlanning;
using System.Diagnostics;

namespace LanternAI.Api.Endpoints;

/// <summary>
/// Promptbooks, anomaly detection, and incident summaries.
/// </summary>
public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Promptbooks: list + execute ---
        app.MapGet("/api/promptbooks", () =>
        {
            var books = PromptbookDefinitions.All.Select(b => new
            {
                b.Id,
                b.Name,
                b.Description,
                b.Category,
                stepCount = b.Steps.Count,
                steps = b.Steps.Select(s => new { s.Question, s.Description, s.MinRowsToContinue, s.Summarize }),
            });
            return Results.Ok(books);
        }).WithName("ListPromptbooks");

        app.MapPost("/api/promptbooks/{id}/execute", async (
            string id,
            IQueryPlanService planService,
            IQueryExecutor executor,
            IEventTableCatalog catalog,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var book = PromptbookDefinitions.All.FirstOrDefault(b => b.Id == id);
            if (book is null)
                return Results.NotFound(new { title = "Promptbook not found", detail = $"No promptbook with id '{id}'." });

            var logger = loggerFactory.CreateLogger("LanternAI.Promptbook");
            var stopwatch = Stopwatch.StartNew();
            var stepResults = new List<PromptbookStepResult>();
            ConversationContext? context = null;
            int totalTokens = 0;

            for (var i = 0; i < book.Steps.Count; i++)
            {
                var step = book.Steps[i];

                // Check continuation condition
                if (i > 0 && step.MinRowsToContinue is { } minRows)
                {
                    var prevRows = stepResults[^1].RowCount;
                    if (prevRows < minRows)
                    {
                        logger.LogInformation("Step {Index} skipped (previous step returned {Rows} rows, need {Min})", i, prevRows, minRows);
                        stepResults.Add(new PromptbookStepResult(i, step.Question, null, null, null, null, 0, Skipped: true));
                        continue;
                    }
                }

                var planned = await planService.BuildPlanWithUsageAsync(step.Question, context, ct);
                var plan = planned.Plan;
                totalTokens += planned.TotalTokens ?? 0;

                var result = executor.Execute(plan);
                var kql = KqlRenderer.Render(plan);

                string? summary = null;
                if (step.Summarize)
                {
                    try
                    {
                        summary = await planService.SummarizeAsync(step.Question, plan, result, ct);
                        totalTokens += summary.Length / 4; // rough token estimate
                    }
                    catch (LlmUnavailableException ex)
                    {
                        logger.LogWarning(ex, "Summary failed for promptbook step {Index}", i);
                    }
                }

                stepResults.Add(new PromptbookStepResult(i, step.Question, plan, result, kql, summary, result.Rows.Count, Skipped: false));

                // Pass context to next step
                context = new ConversationContext(step.Question, plan, summary);
            }

            stopwatch.Stop();
            return Results.Ok(new PromptbookExecutionResult(
                book.Id,
                book.Name,
                stepResults,
                (int)stopwatch.Elapsed.TotalMilliseconds,
                totalTokens));
        }).WithName("ExecutePromptbook");

        // --- Anomaly detection ---
        app.MapPost("/api/analyze/anomalies", (AnomalyRequest request) =>
        {
            var detector = new AnomalyDetector();
            var report = detector.Analyze(request.Plan, request.Result);
            return Results.Ok(report);
        }).WithName("DetectAnomalies");

        // --- Incident summary ---
        app.MapPost("/api/analyze/incident-summary", async (IncidentRequest request, IncidentSummaryService summaryService, CancellationToken ct) =>
        {
            if (request.Queries.Count == 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["queries"] = ["At least one query is required."],
                });

            try
            {
                var summary = await summaryService.GenerateAsync(request.Queries, ct);
                return Results.Ok(summary);
            }
            catch (LlmUnavailableException ex)
            {
                return Results.Problem(ex.Message, statusCode: 503, title: "Language model unavailable");
            }
        }).WithName("GenerateIncidentSummary");
    }

    public sealed record AnomalyRequest(QueryPlan Plan, QueryResult Result);

    public sealed record IncidentRequest(IReadOnlyList<SessionQuery> Queries);
}
