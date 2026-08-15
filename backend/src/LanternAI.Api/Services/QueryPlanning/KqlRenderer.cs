using System.Globalization;
using System.Text;
using LanternAI.Api.Models;

namespace LanternAI.Api.Services.QueryPlanning;

/// <summary>
/// Renders a validated <see cref="QueryPlan"/> as KQL text, purely for
/// display/transparency — this string is never parsed or executed; the
/// plan itself drives execution. See <see cref="SimulatedQueryExecutor"/>
/// (or, later, the real ADX client) for the actual data path.
/// </summary>
public static class KqlRenderer
{
    public static string Render(QueryPlan plan)
    {
        var sourceTables = plan.Tables is { Count: > 0 } ? plan.Tables : [plan.Table];
        var sb = new StringBuilder(sourceTables.Count == 1 ? sourceTables[0] : $"union {string.Join(", ", sourceTables)}");

        if (plan.TimeRange is { } timeRange)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n| where {timeRange.Column} > ago({FormatHours(timeRange.LookbackHours)})");
        }

        foreach (var filter in plan.Filters)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n| where {RenderFilter(filter)}");
        }

        if (plan.Aggregation is { } agg)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n| summarize {RenderAggregation(agg)}");
            if (agg.GroupBy is { Count: > 0 })
            {
                sb.Append(CultureInfo.InvariantCulture, $" by {string.Join(", ", agg.GroupBy)}");
            }
        }
        else if (plan.Columns is { Count: > 0 })
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n| project {string.Join(", ", plan.Columns)}");
        }

        if (plan.Limit is { } limit)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n| take {limit}");
        }

        return sb.ToString();
    }

    private static string RenderFilter(QueryFilter filter)
    {
        var value = IsNumeric(filter.Value) ? filter.Value : $"\"{filter.Value}\"";
        return filter.Operator switch
        {
            FilterOperator.Equals => $"{filter.Column} == {value}",
            FilterOperator.NotEquals => $"{filter.Column} != {value}",
            FilterOperator.Contains => $"{filter.Column} contains {value}",
            FilterOperator.GreaterThan => $"{filter.Column} > {value}",
            FilterOperator.LessThan => $"{filter.Column} < {value}",
            _ => throw new ArgumentOutOfRangeException(nameof(filter)),
        };
    }

    private static string RenderAggregation(QueryAggregation agg) => agg.Function switch
    {
        AggregationFunction.Count => "count()",
        AggregationFunction.Sum => $"sum({agg.Column})",
        AggregationFunction.Avg => $"avg({agg.Column})",
        AggregationFunction.Min => $"min({agg.Column})",
        AggregationFunction.Max => $"max({agg.Column})",
        _ => throw new ArgumentOutOfRangeException(nameof(agg)),
    };

    private static bool IsNumeric(string value) => double.TryParse(value, CultureInfo.InvariantCulture, out _);

    private static string FormatHours(double hours) =>
        hours % 24 == 0 ? $"{(int)(hours / 24)}d" : $"{hours.ToString(CultureInfo.InvariantCulture)}h";
}
