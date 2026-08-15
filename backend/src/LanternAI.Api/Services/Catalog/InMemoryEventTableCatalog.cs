using LanternAI.Api.Data;
using LanternAI.Api.Models;

namespace LanternAI.Api.Services.Catalog;

/// <summary>
/// Phase 1 catalog: serves the fixed set of simulated event tables from
/// <see cref="MockEventData"/>. Registered as a singleton so all requests
/// see the same generated rows for the lifetime of the process.
/// </summary>
public sealed class InMemoryEventTableCatalog : IEventTableCatalog
{
    private readonly Dictionary<string, TableSchema> _schemasByName;
    private readonly Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _rowsByName;

    public InMemoryEventTableCatalog()
    {
        _schemasByName = MockEventData.AllSchemas.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _rowsByName = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            [MockEventData.SignInLogsSchema.Name] = MockEventData.GenerateSignInLogs(),
            [MockEventData.SecurityEventSchema.Name] = MockEventData.GenerateSecurityEvents(),
            [MockEventData.AppRequestsSchema.Name] = MockEventData.GenerateAppRequests(),
            [MockEventData.AuditEventsSchema.Name] = MockEventData.GenerateAuditEvents(),
            [MockEventData.DatabaseQueriesSchema.Name] = MockEventData.GenerateDatabaseQueries(),
            [MockEventData.ApiDependenciesSchema.Name] = MockEventData.GenerateApiDependencies(),
        };
    }

    public IReadOnlyList<TableSchema> GetTables() => _schemasByName.Values.ToList();

    public TableSchema? GetTable(string name) => _schemasByName.GetValueOrDefault(name);

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetRows(string tableName) =>
        _rowsByName.GetValueOrDefault(tableName) ?? [];
}
