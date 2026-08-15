using LanternAI.Api.Models;

namespace LanternAI.Api.Services.Analysis;

/// <summary>
/// Curated multi-step investigation sequences. Each promptbook chains
/// queries where each step builds on the previous result's context.
/// </summary>
public static class PromptbookDefinitions
{
    public static IReadOnlyList<PromptbookDefinition> All { get; } =
    [
        new("brute-force", "Brute-force sign-in investigation",
            "Investigate potential brute-force attacks by analyzing failed sign-ins, grouping by source, and cross-referencing security events.",
            "Identity & Access",
            [
                new("Show me failed signins in the last 24 hours", "Find failed authentication attempts", MinRowsToContinue: 3),
                new("Show me failed signins grouped by IP address", "Identify source IPs with repeated failures", MinRowsToContinue: 3),
                new("Show me security events for accounts with the most failed signins", "Cross-reference with security event logs to find related activity", MinRowsToContinue: 1),
                new("Summarize the findings: which IPs and users are involved, and what does the security event data reveal?", "Generate a consolidated assessment"),
            ]),

        new("app-performance", "Application performance degradation",
            "Investigate slow API responses by endpoint, drill into database queries and dependencies, and identify the bottleneck.",
            "Application Performance",
            [
                new("Average request duration by endpoint in the last 24 hours", "Identify slowest endpoints", MinRowsToContinue: 1),
                new("Show me the slowest API requests", "Find the worst-performing requests", MinRowsToContinue: 1),
                new("Show me slow database queries", "Check if the database is the bottleneck", MinRowsToContinue: 1),
                new("Show me failed dependency calls", "Check if downstream services are failing", Summarize: true),
            ]),

        new("lateral-movement", "Lateral movement detection",
            "Investigate potential lateral movement by analyzing blocked connections, affected services, and container anomalies.",
            "Security Events",
            [
                new("Show me blocked network connections", "Identify denied connections between services", MinRowsToContinue: 1),
                new("Show me network connections to non-standard ports", "Find connections on unusual ports", MinRowsToContinue: 1),
                new("Show me container error logs for services involved in blocked connections", "Check if affected services show errors", MinRowsToContinue: 1),
                new("Show me security events on computers running those services", "Cross-reference with host security logs", Summarize: true),
            ]),

        new("compliance-audit", "Compliance audit trail review",
            "Review denied access attempts, failed compliance jobs, and data export failures to assess regulatory risk.",
            "Compliance & Governance",
            [
                new("Show me denied audit actions in the last 24 hours", "Find access denials on regulated resources", MinRowsToContinue: 1),
                new("Show me failed data exports", "Check for export job failures", MinRowsToContinue: 1),
                new("Show me failed jobs in the last 24 hours", "Check for compliance job failures", MinRowsToContinue: 1),
                new("Summarize the compliance posture: what was denied, what failed, and what is the risk?", "Generate compliance risk assessment", Summarize: true),
            ]),

        new("service-outage", "Service health investigation",
            "Investigate unhealthy services, check for correlated deployment failures and container errors.",
            "Infrastructure & Ops",
            [
                new("Show me unhealthy services", "Identify failing health checks", MinRowsToContinue: 1),
                new("Show me failed deployments for those services", "Check if a recent deployment caused the issue", MinRowsToContinue: 1),
                new("Show me container error logs for unhealthy services", "Check container-level errors", Summarize: true),
            ]),
    ];
}
