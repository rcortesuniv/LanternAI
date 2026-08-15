namespace LanternAI.Api.Models;

/// <summary>
/// Metadata for a (simulated, for now) event table: its name, a short
/// description surfaced in the catalog UI, and its column schema. This is
/// the single source of truth used both to build LLM prompt context and to
/// validate generated query plans, so the model can never reference a table
/// or column that doesn't exist.
/// </summary>
public sealed record TableSchema(string Name, string Description, IReadOnlyList<ColumnSchema> Columns)
{
    public int? RowCount { get; init; }

    public ColumnSchema? FindColumn(string name) =>
        Columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
}
