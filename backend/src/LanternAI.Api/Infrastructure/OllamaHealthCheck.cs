using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using LanternAI.Api.Services.Llm;
using System.Text.Json;

namespace LanternAI.Api.Infrastructure;

public sealed class OllamaHealthCheck(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
            }
            client.Timeout = TimeSpan.FromSeconds(3);
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
                : HealthCheckResult.Unhealthy($"Configured Ollama model '{options.Value.Model}' is not installed.");
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
