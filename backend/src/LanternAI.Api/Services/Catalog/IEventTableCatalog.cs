using LanternAI.Api.Models;

namespace LanternAI.Api.Services.Catalog;

/// <summary>
/// Source of truth for "what tables exist and what's in them". Phase 1 is
/// backed by in-memory fixtures standing in for the cda-db event tables;
/// a future ADX-backed implementation can satisfy the same interface
/// without changing anything upstream (query planning, execution, UI).
/// </summary>
public interface IEventTableCatalog
{
    IReadOnlyList<TableSchema> GetTables();

    TableSchema? GetTable(string name);

    /// <summary>All rows for a table, as column-name -> value dictionaries. Returns empty for an unknown table.</summary>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetRows(string tableName);
}
