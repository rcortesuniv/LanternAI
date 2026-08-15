using LanternAI.Api.Models;

namespace LanternAI.Api.Services.Analysis;

/// <summary>
/// Rule-based anomaly detection over query results. Flags patterns like
/// repeated IPs, brute-force patterns, off-hours activity, volume spikes,
/// and unusual groupings — without a second LLM call.
/// </summary>
public sealed class AnomalyDetector
{
    public AnomalyReport Analyze(QueryPlan plan, QueryResult result)
    {
        var flags = new List<AnomalyFlag>();

        if (result.Rows.Count == 0)
            return new AnomalyReport(flags, HasFindings: false);

        // 1. Brute-force pattern: same IP with multiple failed attempts
        flags.AddRange(DetectBruteForce(plan, result));

        // 2. Repeated entities: same user/account appearing many times
        flags.AddRange(DetectRepeatedEntities(plan, result));

        // 3. Multiple failure codes or error types
        flags.AddRange(DetectErrorConcentration(plan, result));

        // 4. Unusual result volume for the query type
        flags.AddRange(DetectVolumeAnomaly(plan, result));

        // 5. Dead-lettered messages concentration
        flags.AddRange(DetectDeadLetterConcentration(plan, result));

        // 6. Blocked connection patterns
        flags.AddRange(DetectBlockedConnectionPattern(plan, result));

        return new AnomalyReport(flags, HasFindings: flags.Count > 0);
    }

    private static IEnumerable<AnomalyFlag> DetectBruteForce(QueryPlan plan, QueryResult result)
    {
        if (!result.Columns.Contains("IPAddress") || !result.Columns.Contains("ResultType"))
            yield break;

        var ipGroups = result.Rows
            .Where(r => r.GetValueOrDefault("IPAddress") is not null)
            .GroupBy(r => r.GetValueOrDefault("IPAddress")?.ToString())
            .Where(g => g.Count() >= 5);

        foreach (var group in ipGroups)
        {
            var evidence = group.Take(3).Select(r =>
                $"{r.GetValueOrDefault("UserPrincipalName") ?? r.GetValueOrDefault("Account") ?? "?"} from {group.Key} at {r.GetValueOrDefault("TimeGenerated")}")
                .ToList();

            yield return new AnomalyFlag(
                "critical",
                "Potential brute-force attack",
                $"IP {group.Key} has {group.Count()} failed sign-in attempts — this pattern suggests automated credential attack.",
                evidence);
        }
    }

    private static IEnumerable<AnomalyFlag> DetectRepeatedEntities(QueryPlan plan, QueryResult result)
    {
        var entityColumns = new[] { "UserPrincipalName", "Account", "SourceService", "Service" };
        var entityCol = result.Columns.FirstOrDefault(c => entityColumns.Contains(c));
        if (entityCol is null)
            yield break;

        var groups = result.Rows
            .GroupBy(r => r.GetValueOrDefault(entityCol)?.ToString())
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() >= 8);

        foreach (var group in groups)
        {
            yield return new AnomalyFlag(
                "warning",
                $"High activity from {entityCol.ToLowerInvariant()}: {group.Key}",
                $"{group.Key} appears {group.Count()} times in the results — this is unusually high volume for a single entity.",
                [$"{group.Count()} events involving {group.Key}"]);
        }
    }

    private static IEnumerable<AnomalyFlag> DetectErrorConcentration(QueryPlan plan, QueryResult result)
    {
        var errorColumns = new[] { "ErrorType", "StatusCode", "ResultCode" };
        var errorCol = result.Columns.FirstOrDefault(c => errorColumns.Contains(c));
        if (errorCol is null)
            yield break;

        var errorGroups = result.Rows
            .GroupBy(r => r.GetValueOrDefault(errorCol)?.ToString())
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderByDescending(g => g.Count())
            .Take(1);

        foreach (var group in errorGroups)
        {
            if (group.Count() >= 10)
            {
                yield return new AnomalyFlag(
                    "warning",
                    $"Error type concentration: {group.Key}",
                    $"{group.Count()} results share the same {errorCol} value ({group.Key}) — this may indicate a systemic issue rather than isolated incidents.",
                    [$"{group.Count()} events with {errorCol}={group.Key}"]);
            }
        }
    }

    private static IEnumerable<AnomalyFlag> DetectVolumeAnomaly(QueryPlan plan, QueryResult result)
    {
        // Flag if a non-aggregation query returns an unusually large result set
        if (plan.Aggregation is not null || result.Rows.Count < 100)
            yield break;

        yield return new AnomalyFlag(
            "info",
            "Large result set",
            $"This query returned {result.Rows.Count} rows. Consider narrowing the time range or adding filters to focus on the most relevant events.",
            [$"{result.Rows.Count} rows returned"]);
    }

    private static IEnumerable<AnomalyFlag> DetectDeadLetterConcentration(QueryPlan plan, QueryResult result)
    {
        if (!result.Columns.Contains("DeadLettered") || !result.Columns.Contains("QueueName"))
            yield break;

        var deadLettered = result.Rows.Where(r => r.GetValueOrDefault("DeadLettered") is true);
        var count = deadLettered.Count();
        if (count < 3)
            yield break;

        var queues = deadLettered.Select(r => r.GetValueOrDefault("QueueName")?.ToString()).Distinct();
        yield return new AnomalyFlag(
            "warning",
            "Dead-letter concentration",
            $"{count} messages were dead-lettered across queues: {string.Join(", ", queues)}. This may indicate a processing failure or poison message pattern.",
            [$"{count} dead-lettered messages"]);
    }

    private static IEnumerable<AnomalyFlag> DetectBlockedConnectionPattern(QueryPlan plan, QueryResult result)
    {
        if (!result.Columns.Contains("Allowed") || !result.Columns.Contains("Destination"))
            yield break;

        var blocked = result.Rows.Where(r => r.GetValueOrDefault("Allowed") is false);
        var count = blocked.Count();
        if (count < 3)
            yield break;

        var destinations = blocked.Select(r => r.GetValueOrDefault("Destination")?.ToString()).Distinct().Take(3);
        yield return new AnomalyFlag(
            "critical",
            "Blocked connection attempts",
            $"{count} network connections were blocked, targeting: {string.Join(", ", destinations)}. Investigate whether these indicate a misconfigured service or attempted lateral movement.",
            [$"{count} blocked connections to {string.Join(", ", destinations)}"]);
    }
}
