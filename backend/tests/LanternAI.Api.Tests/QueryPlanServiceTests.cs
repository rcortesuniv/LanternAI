using LanternAI.Api.Services.QueryPlanning;
using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Models;
using LanternAI.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LanternAI.Api.Tests;

public class QueryPlanServiceTests
{
    private readonly FixedEventTableCatalog _catalog = new(DateTimeOffset.UtcNow);

    [Fact]
    public async Task BuildPlanAsync_ValidPlan_ReturnsPlanWithNormalizedTableName()
    {
        var llm = new FakeLlmProvider("""{"table": "testevents", "limit": 10}""");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        var plan = await service.BuildPlanAsync("show me test events");

        Assert.Equal("TestEvents", plan.Table); // catalog casing wins over whatever casing the model used
        Assert.Equal(10, plan.Limit);
    }

    [Fact]
    public async Task BuildPlanAsync_JsonWrappedInMarkdownFence_StillParses()
    {
        var llm = new FakeLlmProvider("Sure, here you go:\n```json\n{\"table\": \"TestEvents\"}\n```");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        var plan = await service.BuildPlanAsync("show me test events");

        Assert.Equal("TestEvents", plan.Table);
        Assert.Equal(100, plan.Limit); // default
    }

    [Fact]
    public async Task BuildPlanAsync_UnknownTable_ThrowsInvalidQueryPlan()
    {
        var llm = new FakeLlmProvider("""{"table": "NotARealTable"}""");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        await Assert.ThrowsAsync<InvalidQueryPlanException>(() => service.BuildPlanAsync("anything"));
    }

    [Fact]
    public async Task BuildPlanAsync_UnknownFilterColumn_ThrowsInvalidQueryPlan()
    {
        var llm = new FakeLlmProvider("""
            {"table": "TestEvents", "filters": [{"column": "NotAColumn", "operator": "Equals", "value": "x"}]}
            """);
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        await Assert.ThrowsAsync<InvalidQueryPlanException>(() => service.BuildPlanAsync("anything"));
    }

    [Fact]
    public async Task BuildPlanAsync_NumericFilterValue_IsNormalizedToString()
    {
        var llm = new FakeLlmProvider("""{"table":"TestEvents","filters":[{"column":"Amount","operator":"GreaterThan","value":15}]}""");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        var plan = await service.BuildPlanAsync("events with amount above 15");

        Assert.Equal("15", Assert.Single(plan.Filters).Value);
    }

    [Fact]
    public async Task BuildPlanAsync_NotJson_ThrowsInvalidQueryPlan()
    {
        var llm = new FakeLlmProvider("I'm not sure how to answer that.");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        await Assert.ThrowsAsync<InvalidQueryPlanException>(() => service.BuildPlanAsync("anything"));
    }

    [Fact]
    public async Task BuildPlanAsync_LimitAboveMax_IsClamped()
    {
        var llm = new FakeLlmProvider("""{"table": "TestEvents", "limit": 100000}""");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        var plan = await service.BuildPlanAsync("anything");

        Assert.Equal(500, plan.Limit);
    }

    [Fact]
    public async Task BuildPlanAsync_EmptyQuestion_ThrowsWithoutCallingLlm()
    {
        var llm = new FakeLlmProvider("""{"table": "TestEvents"}""");
        var service = new QueryPlanService(llm, _catalog, NullLogger<QueryPlanService>.Instance);

        await Assert.ThrowsAsync<InvalidQueryPlanException>(() => service.BuildPlanAsync("   "));
        Assert.Null(llm.LastUserPrompt);
    }

    [Fact]
    public async Task BuildPlanAsync_MultipleSources_ValidatesSharedAggregationColumns()
    {
        var llm = new FakeLlmProvider("""{"table":"AppRequests","tables":["AppRequests","DatabaseQueries","ApiDependencies"],"aggregation":{"function":"Sum","column":"DurationMs"}}""");
        var service = new QueryPlanService(llm, new InMemoryEventTableCatalog(), NullLogger<QueryPlanService>.Instance);

        var plan = await service.BuildPlanAsync("total duration across application telemetry");

        Assert.Equal(["AppRequests", "DatabaseQueries", "ApiDependencies"], plan.Tables);
        Assert.Equal(AggregationFunction.Sum, plan.Aggregation!.Function);
        Assert.Equal("DurationMs", plan.Aggregation.Column);
    }
}
