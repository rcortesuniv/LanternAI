using LanternAI.Api.Models;

namespace LanternAI.Api.Data;

/// <summary>
/// Generates deterministic-shaped, randomized-content sample data for the
/// three simulated event tables used in Phase 1. Row timestamps are spread
/// backward from "now" so time-range questions ("in the last 24 hours")
/// behave sensibly in a live demo; the seeded Random keeps the categorical
/// values reproducible across runs.
/// </summary>
public static class MockEventData
{
    private static readonly string[] Users =
    [
        "aharris@contoso.com", "bpatel@contoso.com", "cmiller@contoso.com",
        "dsingh@contoso.com", "egomez@contoso.com", "fchen@contoso.com",
    ];

    private static readonly string[] Apps = ["Salesforce", "ServiceNow", "Workday", "Office365", "InternalPortal"];
    private static readonly string[] Locations = ["US", "GB", "IN", "DE", "SG", "BR"];
    private static readonly string[] ClientApps = ["Browser", "Mobile App", "Desktop Client"];
    private static readonly string[] Computers = ["APP-SRV-01", "APP-SRV-02", "DB-SRV-01", "WEB-SRV-01", "WEB-SRV-02"];
    private static readonly string[] Activities = ["Logon", "Logoff", "Process Created", "Account Locked", "Privilege Use"];
    private static readonly string[] Endpoints = ["/api/orders", "/api/patients", "/api/inventory", "/api/reports", "/api/auth"];
    private static readonly string[] Resources = ["ClinicalTrial-API", "Identity-Provider", "DataLake", "ResearchPortal"];
    private static readonly string[] Databases = ["ClinicalOps", "Analytics", "Inventory", "Identity"];
    private static readonly string[] DependencyTargets = ["payments.api", "identity.api", "warehouse.api", "clinical-db"];

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
        "Application request telemetry: endpoint, response time, and outcome for each HTTP request.",
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
        "Database query telemetry for operational and analytical workloads.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the query completed (UTC)."),
            new ColumnSchema("Database", "string", "Database that processed the query."),
            new ColumnSchema("QueryType", "string", "Read or write operation."),
            new ColumnSchema("DurationMs", "real", "Query duration in milliseconds."),
            new ColumnSchema("Success", "bool", "Whether the database query completed successfully."),
        ]);

    public static TableSchema ApiDependenciesSchema { get; } = new(
        "ApiDependencies",
        "Downstream dependency calls made by application services.",
        [
            new ColumnSchema("TimeGenerated", "datetime", "When the dependency call completed (UTC)."),
            new ColumnSchema("Target", "string", "Downstream service or host called."),
            new ColumnSchema("DependencyType", "string", "HTTP, database, or queue dependency."),
            new ColumnSchema("DurationMs", "real", "Dependency call duration in milliseconds."),
            new ColumnSchema("Success", "bool", "Whether the dependency call succeeded."),
        ]);

    public static IReadOnlyList<TableSchema> AllSchemas { get; } =
        [SignInLogsSchema, SecurityEventSchema, AppRequestsSchema, AuditEventsSchema, DatabaseQueriesSchema, ApiDependenciesSchema];

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateSignInLogs(int count = 25) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["UserPrincipalName"] = Pick(rng, Users),
            ["AppDisplayName"] = Pick(rng, Apps),
            ["IPAddress"] = RandomIp(rng),
            ["Location"] = Pick(rng, Locations),
            ["ClientAppUsed"] = Pick(rng, ClientApps),
            ["ResultType"] = rng.NextDouble() < 0.85 ? 0 : Pick(rng, [50126, 50053, 53003]),
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateSecurityEvents(int count = 20) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Computer"] = Pick(rng, Computers),
            ["EventID"] = Pick(rng, [4624, 4625, 4672, 4720, 4740]),
            ["Activity"] = Pick(rng, Activities),
            ["Account"] = Pick(rng, Users),
            ["LogonType"] = Pick(rng, [2, 3, 10]),
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateAppRequests(int count = 30) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Name"] = Pick(rng, Endpoints),
            ["ResultCode"] = rng.NextDouble() < 0.9 ? "200" : Pick(rng, ["500", "503", "429"]),
            ["DurationMs"] = Math.Round(rng.NextDouble() * 800 + 20, 1),
            ["Success"] = rng.NextDouble() < 0.9,
            ["ClientIP"] = RandomIp(rng),
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateAuditEvents(int count = 24) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["UserPrincipalName"] = Pick(rng, Users),
            ["Action"] = Pick(rng, ["Read", "Export", "Update", "Delete"]),
            ["Resource"] = Pick(rng, Resources),
            ["Outcome"] = rng.NextDouble() < 0.92 ? "Allowed" : "Denied",
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateDatabaseQueries(int count = 28) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Database"] = Pick(rng, Databases),
            ["QueryType"] = Pick(rng, ["Read", "Write"]),
            ["DurationMs"] = Math.Round(rng.NextDouble() * 1200 + 5, 1),
            ["Success"] = rng.NextDouble() < 0.96,
        });

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateApiDependencies(int count = 26) =>
        Generate(count, rng => new Dictionary<string, object?>
        {
            ["TimeGenerated"] = RandomTimestamp(rng),
            ["Target"] = Pick(rng, DependencyTargets),
            ["DependencyType"] = Pick(rng, ["HTTP", "Database", "Queue"]),
            ["DurationMs"] = Math.Round(rng.NextDouble() * 500 + 2, 1),
            ["Success"] = rng.NextDouble() < 0.94,
        });

    private static List<IReadOnlyDictionary<string, object?>> Generate(
        int count, Func<Random, Dictionary<string, object?>> makeRow)
    {
        // Fixed seed keeps categorical values/shape reproducible across runs and tests;
        // only the timestamp anchor (below) moves with real time.
        var rng = new Random(20240601);
        return Enumerable.Range(0, count).Select(_ => (IReadOnlyDictionary<string, object?>)makeRow(rng)).ToList();
    }

    private static DateTimeOffset RandomTimestamp(Random rng) =>
        DateTimeOffset.UtcNow.AddHours(-rng.NextDouble() * 7 * 24);

    private static string RandomIp(Random rng) =>
        $"{rng.Next(1, 255)}.{rng.Next(0, 255)}.{rng.Next(0, 255)}.{rng.Next(1, 255)}";

    private static T Pick<T>(Random rng, IReadOnlyList<T> values) => values[rng.Next(values.Count)];
}
