using LanternAI.Api.Models;
using LanternAI.Api.Services.Execution;
using LanternAI.Api.Tests.TestDoubles;
using Xunit;

namespace LanternAI.Api.Tests;

public class SimulatedQueryExecutorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly SimulatedQueryExecutor _executor = new(new FixedEventTableCatalog(Now));

    [Fact]
    public void Execute_NoFilters_ReturnsAllColumnsAndAllRows()
    {
        var plan = new QueryPlan { Table = "TestEvents", Limit = 100 };

        var result = _executor.Execute(plan);

        Assert.Equal(["Timestamp", "Category", "Amount"], result.Columns);
        Assert.Equal(4, result.Rows.Count);
    }

    [Fact]
    public void Execute_EqualsFilter_ReturnsOnlyMatchingRows()
    {
        var plan = new QueryPlan
        {
            Table = "TestEvents",
            Filters = [new QueryFilter("Category", FilterOperator.Equals, "beta")],
        };

        var result = _executor.Execute(plan);

        var row = Assert.Single(result.Rows);
        Assert.Equal("beta", row["Category"]);
    }

    [Fact]
    public void Execute_GreaterThanFilter_ComparesNumerically()
    {
        var plan = new QueryPlan
        {
            Table = "TestEvents",
            Filters = [new QueryFilter("Amount", FilterOperator.GreaterThan, "15")],
        };

        var result = _executor.Execute(plan);

        Assert.Equal(2, result.Rows.Count); // 20 and 999
    }

    [Fact]
    public void Execute_TimeRange_ExcludesOlderRows()
    {
        var plan = new QueryPlan
        {
            Table = "TestEvents",
            TimeRange = new QueryTimeRange("Timestamp", LookbackHours: 24),
        };

        var result = _executor.Execute(plan);

        Assert.Equal(3, result.Rows.Count); // excludes the 10-day-old row
    }

    [Fact]
    public void Execute_CountAggregationGroupedByCategory_ReturnsOneRowPerGroup()
    {
        var plan = new QueryPlan
        {
            Table = "TestEvents",
            Aggregation = new QueryAggregation(AggregationFunction.Count, Column: null, GroupBy: ["Category"]),
        };

        var result = _executor.Execute(plan);

        Assert.Equal(["Category", "Count"], result.Columns);
        Assert.Equal(2, result.Rows.Count);
        Assert.Contains(result.Rows, r => (string?)r["Category"] == "alpha" && (int)r["Count"]! == 3);
        Assert.Contains(result.Rows, r => (string?)r["Category"] == "beta" && (int)r["Count"]! == 1);
    }

    [Fact]
    public void Execute_SumAggregationNoGroupBy_ReturnsSingleTotal()
    {
        var plan = new QueryPlan
        {
            Table = "TestEvents",
            Aggregation = new QueryAggregation(AggregationFunction.Sum, Column: "Amount", GroupBy: null),
        };

        var result = _executor.Execute(plan);

        var row = Assert.Single(result.Rows);
        Assert.Equal(1034.0, (double)row["Sum_Amount"]!);
    }

    [Fact]
    public void Execute_Limit_CapsRowCount()
    {
        var plan = new QueryPlan { Table = "TestEvents", Limit = 2 };

        var result = _executor.Execute(plan);

        Assert.Equal(2, result.Rows.Count);
    }
}
