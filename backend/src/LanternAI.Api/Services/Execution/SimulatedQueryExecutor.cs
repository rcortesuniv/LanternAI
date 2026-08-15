using System.Globalization;
using LanternAI.Api.Models;
using LanternAI.Api.Services.Catalog;

namespace LanternAI.Api.Services.Execution;

/// <summary>
/// Runs a validated <see cref="QueryPlan"/> against the in-memory mock
/// tables via LINQ. Every table/column referenced by the plan is already
/// known-good (validated in <see cref="QueryPlanning.QueryPlanService"/>),
/// so this class only needs to worry about filtering/aggregating shape, not
/// safety.
/// </summary>
public sealed class SimulatedQueryExecutor(IEventTableCatalog catalog) : IQueryExecutor
{
    public QueryResult Execute(QueryPlan plan)
    {
        var sourceTables = plan.Tables is { Count: > 0 } ? plan.Tables : [plan.Table];
        var table = catalog.GetTable(plan.Table)
            ?? throw new InvalidOperationException($"Table '{plan.Table}' was validated but no longer exists in the catalog.");

        IEnumerable<IReadOnlyDictionary<string, object?>> rows = sourceTables.SelectMany(catalog.GetRows);

        if (plan.TimeRange is { } timeRange)
        {
            var cutoff = DateTimeOffset.UtcNow.AddHours(-timeRange.LookbackHours);
            rows = rows.Where(r => r[timeRange.Column] is DateTimeOffset ts && ts > cutoff);
        }

        foreach (var filter in plan.Filters)
        {
            rows = rows.Where(r => MatchesFilter(r[filter.Column], filter));
        }

        return plan.Aggregation is { } aggregation
            ? ExecuteAggregation(rows, aggregation, plan.Limit)
            : ExecuteProjection(rows, table, plan.Columns, plan.Limit);
    }

    private static QueryResult ExecuteProjection(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows, TableSchema table, IReadOnlyList<string>? requestedColumns, int? limit)
    {
        var columns = requestedColumns is { Count: > 0 } ? requestedColumns : table.Columns.Select(c => c.Name).ToList();
        var limited = limit is { } l ? rows.Take(l) : rows;

        var resultRows = limited
            .Select(r => (IReadOnlyDictionary<string, object?>)columns.ToDictionary(c => c, c => r.GetValueOrDefault(c)))
            .ToList();

        return new QueryResult(columns, resultRows);
    }

    private static QueryResult ExecuteAggregation(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows, QueryAggregation aggregation, int? limit)
    {
        var groupByColumns = aggregation.GroupBy ?? [];
        var resultColumn = AggregateResultColumnName(aggregation);
        var columns = groupByColumns.Concat([resultColumn]).ToList();

        var groups = groupByColumns.Count > 0
            ? rows.GroupBy(r => string.Join("", groupByColumns.Select(c => Convert.ToString(r.GetValueOrDefault(c), CultureInfo.InvariantCulture))))
            : rows.GroupBy(_ => string.Empty);

        var aggregated = groups.Select(group =>
        {
            var groupRows = group.ToList();
            var row = new Dictionary<string, object?>();
            if (groupByColumns.Count > 0)
            {
                var first = groupRows[0];
                foreach (var col in groupByColumns)
                {
                    row[col] = first.GetValueOrDefault(col);
                }
            }
            row[resultColumn] = ComputeAggregate(groupRows, aggregation);
            return (IReadOnlyDictionary<string, object?>)row;
        });

        if (limit is { } l)
        {
            aggregated = aggregated.Take(l);
        }

        return new QueryResult(columns, aggregated.ToList());
    }

    private static object? ComputeAggregate(List<IReadOnlyDictionary<string, object?>> rows, QueryAggregation aggregation)
    {
        if (aggregation.Function == AggregationFunction.Count)
        {
            return rows.Count;
        }

        var values = rows
            .Select(r => ToDouble(r.GetValueOrDefault(aggregation.Column!)))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        return aggregation.Function switch
        {
            AggregationFunction.Sum => values.Sum(),
            AggregationFunction.Avg => values.Count > 0 ? Math.Round(values.Average(), 2) : null,
            AggregationFunction.Min => values.Count > 0 ? values.Min() : null,
            AggregationFunction.Max => values.Count > 0 ? values.Max() : null,
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation)),
        };
    }

    private static string AggregateResultColumnName(QueryAggregation aggregation) => aggregation.Function switch
    {
        AggregationFunction.Count => "Count",
        _ => $"{aggregation.Function}_{aggregation.Column}",
    };

    private static bool MatchesFilter(object? actual, QueryFilter filter)
    {
        if (actual is null)
        {
            return filter.Operator == FilterOperator.NotEquals;
        }

        // Prefer numeric comparison when both sides look numeric; otherwise fall back to text.
        var actualNumber = ToDouble(actual);
        var filterNumber = double.TryParse(filter.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv) ? fv : (double?)null;

        if (actualNumber.HasValue && filterNumber.HasValue)
        {
            return filter.Operator switch
            {
                FilterOperator.Equals => actualNumber == filterNumber,
                FilterOperator.NotEquals => actualNumber != filterNumber,
                FilterOperator.GreaterThan => actualNumber > filterNumber,
                FilterOperator.LessThan => actualNumber < filterNumber,
                FilterOperator.Contains => actual.ToString()!.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
        return filter.Operator switch
        {
            FilterOperator.Equals => string.Equals(actualText, filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOperator.NotEquals => !string.Equals(actualText, filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOperator.Contains => actualText.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            FilterOperator.GreaterThan => string.Compare(actualText, filter.Value, StringComparison.OrdinalIgnoreCase) > 0,
            FilterOperator.LessThan => string.Compare(actualText, filter.Value, StringComparison.OrdinalIgnoreCase) < 0,
            _ => false,
        };
    }

    private static double? ToDouble(object? value) => value switch
    {
        null => null,
        double d => d,
        int i => i,
        long l => l,
        bool b => b ? 1 : 0,
        _ => double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null,
    };
}
