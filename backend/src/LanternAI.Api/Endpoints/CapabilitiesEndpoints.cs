using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Services.Llm;
using Microsoft.Extensions.Options;
using LanternAI.Api.Services.Execution;

namespace LanternAI.Api.Endpoints;

public static class CapabilitiesEndpoints
{
    public static void MapCapabilitiesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/capabilities", (IEventTableCatalog catalog, IOptions<OllamaOptions> ollama, IConfiguration configuration, IDataSourceCapabilitiesProvider sources) =>
        {
            var authConfigured = !string.IsNullOrWhiteSpace(configuration["Authentication:Authority"])
                && !string.IsNullOrWhiteSpace(configuration["Authentication:ClientId"]);
            var adxConfigured = !string.IsNullOrWhiteSpace(configuration["Adx:ClusterUri"])
                && !string.IsNullOrWhiteSpace(configuration["Adx:Database"]);

            return Results.Ok(new
            {
                authentication = new { configured = authConfigured, provider = "Entra ID" },
                data = new { configured = adxConfigured, provider = adxConfigured ? "Azure Data Explorer" : "Simulated catalog" },
                languageModel = new { provider = "Ollama Cloud", model = ollama.Value.Model },
                sourceCount = catalog.GetTables().Count,
                dataSources = sources.GetCapabilities(),
            });
        }).WithName("GetCapabilities");
    }
}
