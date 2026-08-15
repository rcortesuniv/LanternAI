using LanternAI.Api.Models;
using LanternAI.Api.Services.Llm;

namespace LanternAI.Api.Services.Analysis;

/// <summary>
/// Generates a consolidated incident summary from a session of queries
/// by sending the questions, plans, and result summaries to the LLM.
/// </summary>
public sealed class IncidentSummaryService(ILlmProvider llmProvider, ILogger<IncidentSummaryService> logger)
{
    public async Task<IncidentSummary> GenerateAsync(
        IReadOnlyList<SessionQuery> queries,
        CancellationToken cancellationToken = default)
    {
        if (queries.Count == 0)
            throw new ArgumentException("At least one query is required to generate an incident summary.");

        var systemPrompt = """
            You are a security incident analyst. Given a sequence of questions and their results
            from a security investigation, write a structured incident summary.

            Respond with ONLY a JSON object (no prose, no markdown fences) in this shape:
            {
              "title": "<concise incident title>",
              "overview": "<2-3 sentence summary of what was investigated and found>",
              "keyFindings": ["<finding 1>", "<finding 2>", ...],
              "riskAssessment": "<1-2 sentence assessment of severity and potential impact>",
              "recommendedActions": ["<action 1>", "<action 2>", ...]
            }

            Focus on actionable findings, not restating the queries. If the investigation
            found no issues, say so clearly and recommend continued monitoring.
            """;

        var queryDescriptions = queries.Select((q, i) =>
            $"""
            Query {i + 1}: "{q.Question}"
            Table: {q.Plan?.Table ?? "unknown"}
            Rows returned: {q.RowCount}
            Summary: {q.Summary ?? "(no summary)"}
            """);

        var userPrompt = $"""
            Investigation session ({queries.Count} queries):

            {string.Join("\n\n", queryDescriptions)}

            Total rows analyzed across all queries: {queries.Sum(q => q.RowCount)}
            """;

        var completion = await llmProvider.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        return ParseIncidentSummary(completion.Content, queries.Count, queries.Sum(q => q.RowCount));
    }

    private static IncidentSummary ParseIncidentSummary(string llmOutput, int queryCount, int totalRows)
    {
        // Extract JSON from potential prose/fence wrapping
        var json = ExtractJsonObject(llmOutput);
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new IncidentSummary(
                root.GetProperty("title").GetString() ?? "Untitled incident",
                root.GetProperty("overview").GetString() ?? "",
                root.GetProperty("keyFindings").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
                root.GetProperty("riskAssessment").GetString() ?? "",
                root.GetProperty("recommendedActions").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
                queryCount,
                totalRows);
        }
        catch (Exception)
        {
            // Fallback: use raw LLM output as overview
            return new IncidentSummary(
                "Incident summary",
                llmOutput.Trim(),
                [],
                "Unable to parse structured assessment.",
                [],
                queryCount,
                totalRows);
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return text;
        return text[start..(end + 1)];
    }
}

/// <summary>A query from the current session, condensed for the incident summary.</summary>
public sealed record SessionQuery(string Question, QueryPlan? Plan, int RowCount, string? Summary);
