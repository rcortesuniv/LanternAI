using LanternAI.Api.Models;
using LanternAI.Api.Services.Catalog;

namespace LanternAI.Api.Tests.TestDoubles;

/// <summary>A small, hand-authored catalog with deterministic rows — independent of the production mock data generator, for predictable assertions.</summary>
public sealed class FixedEventTableCatalog : IEventTableCatalog
{
    public static readonly TableSchema Schema = new(
        "TestEvents",
        "Fixture table for tests.",
        [
            new ColumnSchema("Timestamp", "datetime", "When the event occurred."),
            new ColumnSchema("Category", "string", "Event category."),
            new ColumnSchema("Amount", "real", "Numeric amount."),
        ]);

    private readonly List<IReadOnlyDictionary<string, object?>> _rows;

    public FixedEventTableCatalog(DateTimeOffset now)
    {
        _rows =
        [
            Row(now.AddHours(-1), "alpha", 10),
            Row(now.AddHours(-2), "alpha", 20),
            Row(now.AddHours(-3), "beta", 5),
            Row(now.AddDays(-10), "alpha", 999), // outside any reasonable lookback window
        ];
    }

    public IReadOnlyList<TableSchema> GetTables() => [Schema];

    public TableSchema? GetTable(string name) =>
        string.Equals(name, Schema.Name, StringComparison.OrdinalIgnoreCase) ? Schema : null;

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetRows(string tableName) =>
        string.Equals(tableName, Schema.Name, StringComparison.OrdinalIgnoreCase) ? _rows : [];

    private static IReadOnlyDictionary<string, object?> Row(DateTimeOffset timestamp, string category, double amount) =>
        new Dictionary<string, object?>
        {
            ["Timestamp"] = timestamp,
            ["Category"] = category,
            ["Amount"] = amount,
        };
}
