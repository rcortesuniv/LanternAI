namespace LanternAI.Api.Models;

/// <summary>
/// Describes a single column of a table, using KQL's scalar type names
/// (datetime, string, int, long, real, bool) so schema context fed to the
/// LLM and the KQL rendered for display both speak the same vocabulary.
/// </summary>
public sealed record ColumnSchema(string Name, string KqlType, string Description);
