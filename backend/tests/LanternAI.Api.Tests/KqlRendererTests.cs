using LanternAI.Api.Models;
using LanternAI.Api.Services.QueryPlanning;
using Xunit;

namespace LanternAI.Api.Tests;

public class KqlRendererTests
{
    [Fact]
    public void Render_FilterAndTimeRangeAndLimit_ProducesExpectedPipeline()
    {
        var plan = new QueryPlan
        {
            Table = "SigninLogs",
            TimeRange = new QueryTimeRange("TimeGenerated", 24),
            Filters = [new QueryFilter("ResultType", FilterOperator.Equals, "0")],
            Columns = ["UserPrincipalName", "AppDisplayName"],
            Limit = 50,
        };

        var kql = KqlRenderer.Render(plan);

        Assert.Equal(
            "SigninLogs\n" +
            "| where TimeGenerated > ago(1d)\n" +
            "| where ResultType == 0\n" +
            "| project UserPrincipalName, AppDisplayName\n" +
            "| take 50",
            kql);
    }

    [Fact]
    public void Render_Aggregation_UsesSummarizeAndOmitsProject()
    {
        var plan = new QueryPlan
        {
            Table = "AppRequests",
            Aggregation = new QueryAggregation(AggregationFunction.Avg, "DurationMs", ["Name"]),
            Limit = 100,
        };

        var kql = KqlRenderer.Render(plan);

        Assert.Equal("AppRequests\n| summarize avg(DurationMs) by Name\n| take 100", kql);
    }

    [Fact]
    public void Render_MultipleSources_UsesUnionBeforeAggregation()
    {
        var plan = new QueryPlan
        {
            Table = "AppRequests",
            Tables = ["AppRequests", "DatabaseQueries", "ApiDependencies"],
            Aggregation = new QueryAggregation(AggregationFunction.Sum, "DurationMs", null),
            Limit = 100,
        };

        var kql = KqlRenderer.Render(plan);

        Assert.Equal("union AppRequests, DatabaseQueries, ApiDependencies\n| summarize sum(DurationMs)\n| take 100", kql);
    }
}
