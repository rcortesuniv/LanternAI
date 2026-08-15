using System.Text.Json;
using System.Text.Json.Serialization;
using LanternAI.Api.Models;
using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Services.Llm;

namespace LanternAI.Api.Services.QueryPlanning;

/// <summary>
/// Prompts the LLM for a structured query plan (JSON), then validates it
/// against the table catalog before anything downstream ever sees it. A
/// plan that references an unknown table/column, or that the model
/// otherwise mangled, is rejected here rather than being executed.
/// </summary>
public sealed class QueryPlanService(ILlmProvider llmProvider, IEventTableCatalog catalog, ILogger<QueryPlanService> logger)
    : IQueryPlanService
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public async Task<QueryPlan> BuildPlanAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidQueryPlanException("Question must not be empty.");
        }

        var systemPrompt = BuildSystemPrompt();
        var rawResponse = await llmProvider.CompleteAsync(systemPrompt, question, cancellationToken);

        var raw = ParseJson(rawResponse);
        return Validate(raw);
    }

    private string BuildSystemPrompt()
    {
        var tableDescriptions = catalog.GetTables().Select(t =>
        {
            var columns = string.Join(", ", t.Columns.Select(c => $"{c.Name} ({c.KqlType}) - {c.Description}"));
            return $"- {t.Name}: {t.Description}\n  Columns: {columns}";
        });

        return $$"""
            You translate a user's natural-language question into a JSON query plan
            over the tables below. Respond with ONLY a single JSON object — no prose,
            no markdown fences.

            Available tables:
            {{string.Join("\n", tableDescriptions)}}

                        JSON shape (omit fields you don't need):
            {
              "table": "<one of the table names above>",
                            "tables": ["<one or more table names to union for cross-source analysis>"],
              "columns": ["<column names to return, or omit for all columns>"],
              "filters": [{"column": "<name>", "operator": "Equals|NotEquals|Contains|GreaterThan|LessThan", "value": "<string>"}],
              "timeRange": {"column": "<a datetime column>", "lookbackHours": <number>},
              "aggregation": {"function": "Count|Sum|Avg|Min|Max", "column": "<numeric column, omit for Count>", "groupBy": ["<column names>"]},
              "limit": <integer, defaults to 100, max 500>
            }

            Rules:
            - Only use table and column names exactly as listed above.
            - "table" is required and is the primary source; use "tables" when the question asks to combine sources.
            - Cross-source plans are unioned before filtering and aggregation. Only use columns shared by every selected table.
                        - Always return a JSON object with a non-empty "table", even when the question is ambiguous.
                        - Example cross-source request: "total duration across app requests, database queries, and API dependencies"
                            -> table=AppRequests; tables=AppRequests,DatabaseQueries,ApiDependencies; aggregation=Sum(DurationMs)
            - If the question doesn't map to any table, still pick the closest table —
              validation on our side will reject anything unusable.
            """;
    }

    private RawPlan ParseJson(string llmOutput)
    {
        var jsonText = ExtractJsonObject(llmOutput);
        try
        {
            var raw = JsonSerializer.Deserialize<RawPlan>(jsonText, JsonOptions);
            if (raw is null || string.IsNullOrWhiteSpace(raw.Table))
            {
                throw new InvalidQueryPlanException("The model did not return a usable query plan.");
            }
            return raw;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse LLM response as JSON: {Response}", llmOutput);
            throw new InvalidQueryPlanException("The model's response could not be understood as a query plan.");
        }
    }

    /// <summary>Models occasionally wrap JSON in prose or ```json fences despite instructions; salvage the object.</summary>
    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidQueryPlanException("The model's response did not contain a JSON object.");
        }
        return text[start..(end + 1)];
    }

    private QueryPlan Validate(RawPlan raw)
    {
        var sourceNames = (raw.Tables is { Count: > 0 } ? raw.Tables : [raw.Table]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sourceTables = sourceNames.Select(name => catalog.GetTable(name)
            ?? throw new InvalidQueryPlanException($"Unknown table '{name}'.")).ToList();
        var table = sourceTables[0];

        var columns = raw.Columns?.Count > 0 ? ValidateSharedColumns(sourceTables, raw.Columns) : null;
        var filters = (raw.Filters ?? []).Select(f => ValidateSharedFilter(sourceTables, f)).ToList();
        var timeRange = raw.TimeRange is null ? null : ValidateSharedTimeRange(sourceTables, raw.TimeRange);
        var aggregation = raw.Aggregation is null ? null : ValidateSharedAggregation(sourceTables, raw.Aggregation);
        var limit = Math.Clamp(raw.Limit ?? DefaultLimit, 1, MaxLimit);

        return new QueryPlan
        {
            Table = table.Name,
            Tables = sourceNames.Count > 1 ? sourceTables.Select(t => t.Name).ToList() : null,
            Columns = columns,
            Filters = filters,
            TimeRange = timeRange,
            Aggregation = aggregation,
            Limit = limit,
        };
    }

    private static List<string> ValidateColumns(TableSchema table, IEnumerable<string> columns)
    {
        var result = new List<string>();
        foreach (var name in columns)
        {
            var column = table.FindColumn(name)
                ?? throw new InvalidQueryPlanException($"Unknown column '{name}' on table '{table.Name}'.");
            result.Add(column.Name);
        }
        return result;
    }

    private static List<string> ValidateSharedColumns(IReadOnlyList<TableSchema> tables, IEnumerable<string> columns) =>
        columns.Select(column => tables.Select(table => table.FindColumn(column)
            ?? throw new InvalidQueryPlanException($"Unknown shared column '{column}'.")).First().Name).ToList();

    private static QueryFilter ValidateSharedFilter(IReadOnlyList<TableSchema> tables, RawFilter filter)
    {
        var column = ValidateSharedColumns(tables, [filter.Column]).Single();
        if (!Enum.TryParse<FilterOperator>(filter.Operator, ignoreCase: true, out var op))
        {
            throw new InvalidQueryPlanException($"Unknown filter operator '{filter.Operator}'.");
        }
        return new QueryFilter(column, op, filter.Value);
    }

    private static QueryFilter ValidateFilter(TableSchema table, RawFilter filter)
    {
        var column = table.FindColumn(filter.Column)
            ?? throw new InvalidQueryPlanException($"Unknown filter column '{filter.Column}' on table '{table.Name}'.");
        if (!Enum.TryParse<FilterOperator>(filter.Operator, ignoreCase: true, out var op))
        {
            throw new InvalidQueryPlanException($"Unknown filter operator '{filter.Operator}'.");
        }
        return new QueryFilter(column.Name, op, filter.Value);
    }

    private static QueryTimeRange ValidateTimeRange(TableSchema table, RawTimeRange timeRange)
    {
        var column = table.FindColumn(timeRange.Column)
            ?? throw new InvalidQueryPlanException($"Unknown time range column '{timeRange.Column}' on table '{table.Name}'.");
        if (timeRange.LookbackHours <= 0)
        {
            throw new InvalidQueryPlanException("timeRange.lookbackHours must be positive.");
        }
        return new QueryTimeRange(column.Name, timeRange.LookbackHours);
    }

    private static QueryTimeRange ValidateSharedTimeRange(IReadOnlyList<TableSchema> tables, RawTimeRange timeRange)
    {
        var column = ValidateSharedColumns(tables, [timeRange.Column]).Single();
        if (timeRange.LookbackHours <= 0)
        {
            throw new InvalidQueryPlanException("timeRange.lookbackHours must be positive.");
        }
        return new QueryTimeRange(column, timeRange.LookbackHours);
    }

    private static QueryAggregation ValidateAggregation(TableSchema table, RawAggregation aggregation)
    {
        if (!Enum.TryParse<AggregationFunction>(aggregation.Function, ignoreCase: true, out var function))
        {
            throw new InvalidQueryPlanException($"Unknown aggregation function '{aggregation.Function}'.");
        }

        string? column = null;
        if (function != AggregationFunction.Count)
        {
            column = (table.FindColumn(aggregation.Column ?? string.Empty)
                ?? throw new InvalidQueryPlanException($"Aggregation '{function}' requires a valid numeric column.")).Name;
        }

        var groupBy = aggregation.GroupBy?.Count > 0 ? ValidateColumns(table, aggregation.GroupBy) : null;
        return new QueryAggregation(function, column, groupBy);
    }

    private static QueryAggregation ValidateSharedAggregation(IReadOnlyList<TableSchema> tables, RawAggregation aggregation)
    {
        if (!Enum.TryParse<AggregationFunction>(aggregation.Function, ignoreCase: true, out var function))
        {
            throw new InvalidQueryPlanException($"Unknown aggregation function '{aggregation.Function}'.");
        }

        string? column = null;
        if (function != AggregationFunction.Count)
        {
            column = ValidateSharedColumns(tables, [aggregation.Column ?? string.Empty]).Single();
        }

        var groupBy = aggregation.GroupBy?.Count > 0 ? ValidateSharedColumns(tables, aggregation.GroupBy) : null;
        return new QueryAggregation(function, column, groupBy);
    }

    private sealed record RawPlan(
        string Table,
        List<string>? Tables,
        List<string>? Columns,
        List<RawFilter>? Filters,
        RawTimeRange? TimeRange,
        RawAggregation? Aggregation,
        int? Limit);

    private sealed record RawFilter(string Column, string Operator, string Value);

    private sealed record RawTimeRange(string Column, double LookbackHours);

    private sealed record RawAggregation(string Function, string? Column, List<string>? GroupBy);
}
