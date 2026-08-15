namespace LanternAI.Api.Services.Execution;

public enum DataSourceKind { Simulated, AzureDataExplorer }

public sealed record DataSourceCapabilities(string Name, DataSourceKind Kind, bool SupportsJoins, bool SupportsAggregations, bool SupportsCaching);

public interface IDataSourceCapabilitiesProvider
{
    IReadOnlyList<DataSourceCapabilities> GetCapabilities();
}

public sealed class SimulatedDataSourceCapabilitiesProvider : IDataSourceCapabilitiesProvider
{
    public IReadOnlyList<DataSourceCapabilities> GetCapabilities() =>
        [new("simulated-catalog", DataSourceKind.Simulated, SupportsJoins: false, SupportsAggregations: true, SupportsCaching: true)];
}