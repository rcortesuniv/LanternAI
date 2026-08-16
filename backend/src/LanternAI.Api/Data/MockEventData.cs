using LanternAI.Api.Models;

namespace LanternAI.Api.Data;

/// <summary>
/// Generates deterministic-shaped, randomized-content sample data for the
/// simulated event tables. Row timestamps are spread backward from "now"
/// so time-range questions behave sensibly in a live demo; the seeded Random
/// keeps the categorical values reproducible across runs.
/// </summary>
public static class MockEventData
{
    private static readonly string[] Users =
    [
        "aharris@contoso.com", "bpatel@contoso.com", "cmiller@contoso.com",
        "dsingh@contoso.com", "egomez@contoso.com", "fchen@contoso.com",
        "jdoe@contoso.com", "klee@contoso.com", "mwang@contoso.com", "nadia@contoso.com",
        "osilva@contoso.com", "rkhan@contoso.com",
    ];

    private static readonly string[] Apps = ["Salesforce", "ServiceNow", "Workday", "Office365", "InternalPortal", "Confluence", "GitHub"];
    private static readonly string[] Locations = ["US", "GB", "IN", "DE", "SG", "BR", "FR", "JP", "AU", "CA"];
    private static readonly string[] ClientApps = ["Browser", "Mobile App", "Desktop Client", "PowerShell", "SDK"];
    private static readonly string[] Computers = ["APP-SRV-01", "APP-SRV-02", "DB-SRV-01", "DB-SRV-02", "WEB-SRV-01", "WEB-SRV-02", "BASTION-01"];
    private static readonly string[] Activities = ["Logon", "Logoff", "Process Created", "Account Locked", "Privilege Use", "Password Change", "Service Started"];
    private static readonly string[] Endpoints = ["/api/orders", "/api/patients", "/api/inventory", "/api/reports", "/api/auth", "/api/exports", "/api/trials"];
    private static readonly string[] Resources = ["ClinicalTrial-API", "Identity-Provider", "DataLake", "ResearchPortal", "ComplianceVault"];
    private static readonly string[] Databases = ["ClinicalOps", "Analytics", "Inventory", "Identity", "AuditLog"];
    private static readonly string[] DependencyTargets = ["payments.api", "identity.api", "warehouse.api", "clinical-db", "audit-svc", "notify.api"];
    private static readonly string[] Services = ["identity", "orders", "research", "inventory", "reporting", "audit", "notifications"];
    private static readonly string[] Regions = ["eastus", "westeurope", "southeastasia", "uksouth", "northeurope", "westus2"];
    private static readonly string[] Queues = ["clinical-events", "audit-events", "notifications", "data-export", "compliance-checks"];
    private static readonly string[] JobNames = ["NightlyIngest", "TrialMetrics", "ComplianceExport", "IndexRefresh", "ThreatScan", "PolicyCheck"];

    public static TableSchema SignInLogsSchema { get; } = new(
        "SigninLogs",
        "Azure AD sign-in activity: who signed in, from where, to which app, and whether it succeeded.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the sign-in occurred (UTC)."),
            new ColumnSchema("UserPrincipalName", "string", "Signed-in user's UPN."),
            new ColumnSchema("AppDisplayName", "string", "Application the user signed into."),
            new ColumnSchema("IPAddress", "string", "Source IP address of the sign-in."),
            new ColumnSchema("Location", "string", "Two-letter country code the sign-in originated from."),
            new ColumnSchema("ClientAppUsed", "string", "Client used to sign in (browser, mobile, desktop)."),
            new ColumnSchema("ResultType", "int", "0 indicates success; any non-zero value is a failure code."),
        ]);

    public static TableSchema SecurityEventSchema { get; } = new(
        "SecurityEvent",
        "Windows security event log entries collected from monitored servers.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the event was logged (UTC)."),
            new ColumnSchema("Computer", "string", "Host that generated the event."),
            new ColumnSchema("EventID", "int", "Windows event ID."),
            new ColumnSchema("Activity", "string", "Human-readable description of the event."),
            new ColumnSchema("Account", "string", "Account associated with the event."),
            new ColumnSchema("LogonType", "int", "Windows logon type code (2=interactive, 3=network, 10=remote)."),
        ]);

    public static TableSchema AppRequestsSchema { get; } = new(
        "AppRequests",
        "Application security gateway telemetry: endpoint access, response time, and authorization outcome for each HTTP request.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the request was received (UTC)."),
            new ColumnSchema("Name", "string", "Endpoint/route that handled the request."),
            new ColumnSchema("ResultCode", "string", "HTTP status code returned."),
            new ColumnSchema("DurationMs", "real", "Request duration in milliseconds."),
            new ColumnSchema("Success", "bool", "Whether the request completed without error."),
            new ColumnSchema("ClientIP", "string", "Source IP address of the caller."),
        ]);

    public static TableSchema AuditEventsSchema { get; } = new(
        "AuditEvents",
        "Application audit trail for actions taken against regulated resources.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the audit event occurred (UTC)."),
            new ColumnSchema("UserPrincipalName", "string", "User who performed the action."),
            new ColumnSchema("Action", "string", "Action performed against the resource."),
            new ColumnSchema("Resource", "string", "Resource affected by the action."),
            new ColumnSchema("Outcome", "string", "Whether the action was allowed or denied."),
        ]);

    public static TableSchema DatabaseQueriesSchema { get; } = new(
        "DatabaseQueries",
        "Database security audit telemetry for protected operational and analytical workloads.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the query completed (UTC)."),
            new ColumnSchema("Database", "string", "Database that processed the query."),
            new ColumnSchema("QueryType", "string", "Read or write operation."),
            new ColumnSchema("DurationMs", "real", "Query duration in milliseconds."),
            new ColumnSchema("Success", "bool", "Whether the database query completed successfully."),
        ]);

    public static TableSchema ApiDependenciesSchema { get; } = new(
        "ApiDependencies",
        "Security telemetry for downstream dependency calls, trust boundaries, and service-to-service access.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the dependency call completed (UTC)."),
            new ColumnSchema("Target", "string", "Downstream service or host called."),
            new ColumnSchema("DependencyType", "string", "HTTP, database, or queue dependency."),
            new ColumnSchema("DurationMs", "real", "Dependency call duration in milliseconds."),
            new ColumnSchema("Success", "bool", "Whether the dependency call succeeded."),
        ]);

    public static TableSchema DeploymentEventsSchema { get; } = new("DeploymentEvents", "Security deployment history for releases, environments, and production change controls.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the deployment completed (UTC)."), new ColumnSchema("Service", "string", "Service being deployed."), new ColumnSchema("Environment", "string", "Target environment."), new ColumnSchema("Version", "string", "Released application version."), new ColumnSchema("Status", "string", "Deployment outcome.")]);
    public static TableSchema ServiceHealthSchema { get; } = new("ServiceHealth", "Security health checks for platform services, regions, and protected workloads.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the health check ran (UTC)."), new ColumnSchema("Service", "string", "Service being checked."), new ColumnSchema("Region", "string", "Cloud region of the check."), new ColumnSchema("LatencyMs", "real", "Observed check latency."), new ColumnSchema("Healthy", "bool", "Whether the service passed the check.")]);
    public static TableSchema QueueMessagesSchema { get; } = new("QueueMessages", "Security telemetry for asynchronous message processing, dead letters, and trusted queues.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the message was processed (UTC)."), new ColumnSchema("QueueName", "string", "Queue that carried the message."), new ColumnSchema("MessageType", "string", "Logical message type."), new ColumnSchema("ProcessingMs", "real", "Message processing duration."), new ColumnSchema("DeadLettered", "bool", "Whether processing moved to dead letter.")]);
    public static TableSchema ContainerLogsSchema { get; } = new("ContainerLogs", "Container security logs for workload behavior, warnings, failures, and runtime regions.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the log was emitted (UTC)."), new ColumnSchema("Service", "string", "Container service name."), new ColumnSchema("Level", "string", "Log severity."), new ColumnSchema("Message", "string", "Log message text."), new ColumnSchema("Region", "string", "Container region.")]);
    public static TableSchema FeatureFlagsSchema { get; } = new("FeatureFlags", "Security feature-flag evaluations for adaptive access, controlled rollouts, and experiments.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the flag was evaluated (UTC)."), new ColumnSchema("FlagName", "string", "Feature flag identifier."), new ColumnSchema("UserPrincipalName", "string", "User receiving the evaluation."), new ColumnSchema("Enabled", "bool", "Whether the feature was enabled."), new ColumnSchema("Variant", "string", "Selected experiment variant.")]);
    public static TableSchema UserSessionsSchema { get; } = new("UserSessions", "User access-session lifecycle telemetry for authentication, duration, and termination analysis.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the session event occurred (UTC)."), new ColumnSchema("UserPrincipalName", "string", "User associated with the session."), new ColumnSchema("SessionId", "string", "Session identifier."), new ColumnSchema("DurationMin", "real", "Session duration in minutes."), new ColumnSchema("Terminated", "bool", "Whether the session ended normally.")]);
    public static TableSchema DataExportsSchema { get; } = new("DataExports", "Governed security and compliance export jobs for regulated reporting workflows.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the export completed (UTC)."), new ColumnSchema("ExportName", "string", "Export job name."), new ColumnSchema("RequestedBy", "string", "User who requested the export."), new ColumnSchema("RowsExported", "int", "Number of rows written."), new ColumnSchema("Status", "string", "Export outcome.")]);
    public static TableSchema ApiErrorsSchema { get; } = new("ApiErrors", "Security-relevant API failures, authorization errors, abuse signals, and correlation context.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the error occurred (UTC)."), new ColumnSchema("Route", "string", "API route that failed."), new ColumnSchema("StatusCode", "int", "HTTP status code."), new ColumnSchema("ErrorType", "string", "Normalized error category."), new ColumnSchema("Service", "string", "Service that returned the error.")]);
    public static TableSchema JobRunsSchema { get; } = new("JobRuns", "Security and compliance job execution history for scans, policy checks, and evidence collection.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the job completed (UTC)."), new ColumnSchema("JobName", "string", "Background job name."), new ColumnSchema("DurationMs", "real", "Job duration."), new ColumnSchema("Succeeded", "bool", "Whether the job completed successfully."), new ColumnSchema("ItemsProcessed", "int", "Items processed by the job.")]);
    public static TableSchema NetworkConnectionsSchema { get; } = new("NetworkConnections", "Network security connection telemetry between application workloads and protected destinations.", [
        new ColumnSchema("TimeGenerated", "datetime", "When the connection was observed (UTC)."), new ColumnSchema("SourceService", "string", "Originating service."), new ColumnSchema("Destination", "string", "Destination host or service."), new ColumnSchema("Port", "int", "Destination port."), new ColumnSchema("Allowed", "bool", "Whether the connection was allowed.")]);

    public static IReadOnlyList<TableSchema> AllSchemas { get; } =
        [SignInLogsSchema, SecurityEventSchema, AppRequestsSchema, AuditEventsSchema, DatabaseQueriesSchema, ApiDependenciesSchema, DeploymentEventsSchema, ServiceHealthSchema, QueueMessagesSchema, ContainerLogsSchema, FeatureFlagsSchema, UserSessionsSchema, DataExportsSchema, ApiErrorsSchema, JobRunsSchema, NetworkConnectionsSchema];

    // --- Data generation ---
    // Row counts are tuned so that even with a 24h time window (default picker),
    // filtered queries return meaningful results. Timestamps spread over 30 days.

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateSignInLogs(int count = 300) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["UserPrincipalName"] = Pick(rng, Users),
            ["AppDisplayName"] = Pick(rng, Apps),
            ["IPAddress"] = RandomIp(rng),
            ["Location"] = Pick(rng, Locations),
            ["ClientAppUsed"] = Pick(rng, ClientApps),
            ["ResultType"] = rng.NextDouble() < 0.70 ? 0 : Pick(rng, [50126, 50053, 53003, 50125]),
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateSecurityEvents(int count = 250) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Computer"] = Pick(rng, Computers),
            ["EventID"] = Pick(rng, [4624, 4624, 4625, 4634, 4672, 4720, 4740, 4688]),
            ["Activity"] = Pick(rng, Activities),
            ["Account"] = Pick(rng, Users),
            ["LogonType"] = Pick(rng, [2, 3, 3, 10]),
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateAppRequests(int count = 400) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Name"] = Pick(rng, Endpoints),
            ["ResultCode"] = rng.NextDouble() < 0.82 ? "200" : Pick(rng, ["500", "503", "429", "401", "403"]),
            ["DurationMs"] = Math.Round(rng.NextDouble() * 1200 + 15, 1),
            ["Success"] = rng.NextDouble() < 0.82,
            ["ClientIP"] = RandomIp(rng),
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateAuditEvents(int count = 200) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["UserPrincipalName"] = Pick(rng, Users),
            ["Action"] = Pick(rng, ["Read", "Read", "Export", "Update", "Delete"]),
            ["Resource"] = Pick(rng, Resources),
            ["Outcome"] = rng.NextDouble() < 0.85 ? "Allowed" : "Denied",
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateDatabaseQueries(int count = 300) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Database"] = Pick(rng, Databases),
            ["QueryType"] = Pick(rng, ["Read", "Read", "Write"]),
            ["DurationMs"] = Math.Round(rng.NextDouble() * 2000 + 5, 1),
            ["Success"] = rng.NextDouble() < 0.88,
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateApiDependencies(int count = 350) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Target"] = Pick(rng, DependencyTargets),
            ["DependencyType"] = Pick(rng, ["HTTP", "HTTP", "Database", "Queue"]),
            ["DurationMs"] = Math.Round(rng.NextDouble() * 800 + 2, 1),
            ["Success"] = rng.NextDouble() < 0.90,
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateDeploymentEvents(int count = 120) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["Service"] = Pick(rng, Services), ["Environment"] = Pick(rng, ["dev", "staging", "production", "production"]), ["Version"] = $"2026.08.{rng.Next(1, 30)}", ["Status"] = rng.NextDouble() < .82 ? "Succeeded" : "Failed" });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateServiceHealth(int count = 280) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["Service"] = Pick(rng, Services), ["Region"] = Pick(rng, Regions), ["LatencyMs"] = Math.Round(rng.NextDouble() * 400 + 5, 1), ["Healthy"] = rng.NextDouble() < .88 });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateQueueMessages(int count = 320) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["QueueName"] = Pick(rng, Queues), ["MessageType"] = Pick(rng, ["Created", "Updated", "Deleted", "Notification"]), ["ProcessingMs"] = Math.Round(rng.NextDouble() * 500 + 3, 1), ["DeadLettered"] = rng.NextDouble() < .10 });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateContainerLogs(int count = 350) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["Service"] = Pick(rng, Services), ["Level"] = Pick(rng, ["Info", "Info", "Info", "Warning", "Warning", "Error"]), ["Message"] = Pick(rng, ["Request completed", "Retry scheduled", "Dependency timeout", "Configuration loaded", "Health check passed", "Connection refused", "Memory limit exceeded"]), ["Region"] = Pick(rng, Regions) });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateFeatureFlags(int count = 180) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["FlagName"] = Pick(rng, ["new-search", "export-v2", "clinical-dashboard", "adaptive-auth", "audit-trail-v2"]), ["UserPrincipalName"] = Pick(rng, Users), ["Enabled"] = rng.Next(2) == 1, ["Variant"] = Pick(rng, ["control", "treatment-a", "treatment-b", "treatment-c"]) });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateUserSessions(int count = 250) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["UserPrincipalName"] = Pick(rng, Users), ["SessionId"] = $"sess-{rng.Next(10000, 99999)}", ["DurationMin"] = Math.Round(rng.NextDouble() * 240 + 2, 1), ["Terminated"] = rng.NextDouble() < .85 });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateDataExports(int count = 150) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["ExportName"] = Pick(rng, ["TrialSummary", "AccessReview", "UsageReport", "AuditExtract", "ComplianceArchive"]), ["RequestedBy"] = Pick(rng, Users), ["RowsExported"] = rng.Next(100, 80000), ["Status"] = rng.NextDouble() < .85 ? "Completed" : "Failed" });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateApiErrors(int count = 280) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["Route"] = Pick(rng, Endpoints), ["StatusCode"] = Pick(rng, [400, 401, 403, 404, 429, 500, 503]), ["ErrorType"] = Pick(rng, ["Validation", "Unauthorized", "NotFound", "Timeout", "Dependency", "RateLimit"]), ["Service"] = Pick(rng, Services) });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateJobRuns(int count = 200) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["JobName"] = Pick(rng, JobNames), ["DurationMs"] = Math.Round(rng.NextDouble() * 5000 + 20, 1), ["Succeeded"] = rng.NextDouble() < .85, ["ItemsProcessed"] = rng.Next(10, 50000) });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateNetworkConnections(int count = 300) =>
        Generate(count, rng => new() { ["TimeGenerated"] = RandomTimestamp(rng), ["SourceService"] = Pick(rng, Services), ["Destination"] = Pick(rng, DependencyTargets), ["Port"] = Pick(rng, [443, 443, 443, 5432, 6379, 5672, 8080]), ["Allowed"] = rng.NextDouble() < .90 });

    private static List<IReadOnlyDictionary<string, object?>> Generate(
        int count, Func<Random, Dictionary<string, object?>> makeRow)
    {
        // Fixed seed keeps categorical values/shape reproducible across runs and tests;
        // only the timestamp anchor (below) moves with real time.
        var rng = new Random(20240601);
        return Enumerable.Range(0, count).Select(_ => (IReadOnlyDictionary<string, object?>)makeRow(rng)).ToList();
    }

    // Spread timestamps over 30 days so 24h/7d/30d time windows all return meaningful data.
    private static DateTimeOffset RandomTimestamp(Random rng) =>
        DateTimeOffset.UtcNow.AddHours(-rng.NextDouble() * 7 * 24);

    private static string RandomIp(Random rng) =>
        $"{rng.Next(1, 255)}.{rng.Next(0, 255)}.{rng.Next(0, 255)}.{rng.Next(1, 255)}";

    private static T Pick<T>(Random rng, IReadOnlyList<T> values) => values[rng.Next(values.Count)];
}
