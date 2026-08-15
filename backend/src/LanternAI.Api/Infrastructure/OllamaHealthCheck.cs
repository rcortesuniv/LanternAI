using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using LanternAI.Api.Services.Llm;
using System.Text.Json;

namespace LanternAI.Api.Infrastructure;

/// <summary>
/// Probes the Ollama endpoint (local or cloud) via api/tags. Uses a named
/// HttpClient ("ollama-health") configured in Program.cs so BaseAddress,
/// auth headers, and timeout stay in sync with the typed LLM client.
/// </summary>
public sealed class OllamaHealthCheck(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options) : IHealthCheck
{
    private const string HttpClientName = "ollama-health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync("api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy($"Ollama returned {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var modelAvailable = document.RootElement.TryGetProperty("models", out var models)
                && models.EnumerateArray().Any(model => model.TryGetProperty("name", out var name)
                    && ModelMatches(name.GetString(), options.Value.Model));
            return modelAvailable
                ? HealthCheckResult.Healthy("Ollama and the configured model are ready.")
                : HealthCheckResult.Degraded($"Ollama is reachable but model '{options.Value.Model}' was not found in local tags. It may be a remote or cloud-hosted model that is pulled on first use.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return HealthCheckResult.Unhealthy("Ollama is unavailable.");
        }
    }

    private static bool ModelMatches(string? installedName, string configuredName) =>
        string.Equals(installedName, configuredName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(installedName?.Split(':')[0], configuredName.Split(':')[0], StringComparison.OrdinalIgnoreCase);
}
