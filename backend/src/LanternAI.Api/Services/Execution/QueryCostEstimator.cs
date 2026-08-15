using LanternAI.Api.Models;
using LanternAI.Api.Services.Catalog;

namespace LanternAI.Api.Services.Execution;

public sealed record QueryCostEstimate(string Tier, int EstimatedRowsScanned, double EstimatedWorkUnits, string Explanation);

public sealed class QueryCostEstimator(IEventTableCatalog catalog)
{
    public QueryCostEstimate Estimate(QueryPlan plan)
    {
        var sources = plan.Tables is { Count: > 0 } ? plan.Tables : [plan.Table];
        var estimatedRows = sources.Sum(source => catalog.GetRows(source).Count);
        var workUnits = estimatedRows * Math.Max(1, plan.Filters.Count + (plan.Aggregation is null ? 0 : 2));
        var tier = workUnits < 100 ? "low" : workUnits < 1000 ? "medium" : "high";
        return new QueryCostEstimate(tier, estimatedRows, workUnits, $"Estimated from {sources.Count} source(s), {plan.Filters.Count} filter(s), and {(plan.Aggregation is null ? "no" : "an")} aggregation.");
    }
}