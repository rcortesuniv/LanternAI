using LanternAI.Api.Services.Catalog;

namespace LanternAI.Api.Endpoints;

/// <summary>Table catalog endpoints — independent of the query flow, used to populate the UI's catalog panel.</summary>
public static class TablesEndpoints
{
    public static void MapTablesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tables").WithTags("Tables");

        group.MapGet("/", (IEventTableCatalog catalog) => Results.Ok(catalog.GetTables()))
            .WithName("ListTables")
            .WithSummary("List available (simulated) event tables and their schemas.");

        group.MapGet("/{name}", (string name, IEventTableCatalog catalog) =>
            catalog.GetTable(name) is { } table ? Results.Ok(table) : Results.NotFound())
            .WithName("GetTable")
            .WithSummary("Get schema detail for a single table.");
    }
}
