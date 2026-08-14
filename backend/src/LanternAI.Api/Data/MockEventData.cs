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

    public static IReadOnlyList<TableSchema> AllSchemas { get; } =
        [SignInLogsSchema, SecurityEventSchema, AppRequestsSchema];

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
