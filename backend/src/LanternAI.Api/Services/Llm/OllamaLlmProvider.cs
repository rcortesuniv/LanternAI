using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LanternAI.Api.Services.Llm;

/// <summary>
/// Calls a local Ollama instance's chat API. Registered as a typed HTTP
/// client (see Program.cs) so <see cref="HttpClient.BaseAddress"/> and the
/// timeout come from <see cref="OllamaOptions"/>.
/// </summary>
public sealed class OllamaLlmProvider(HttpClient httpClient, IOptions<OllamaOptions> options, ILogger<OllamaLlmProvider> logger)
    : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest(
            options.Value.Model,
            [new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt)],
            Stream: false);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("api/chat", request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to reach Ollama at {BaseAddress}", httpClient.BaseAddress);
            throw new LlmUnavailableException("Could not reach the local Ollama server. Is it running?", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Ollama returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new LlmUnavailableException($"Ollama request failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);
        return payload?.Message?.Content
            ?? throw new LlmUnavailableException("Ollama returned an empty response.");
    }

    private sealed record ChatRequest(string Model, IReadOnlyList<ChatMessage> Messages, bool Stream);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatResponse([property: JsonPropertyName("message")] ChatMessage? Message);
}

/// <summary>Thrown when the configured LLM provider cannot be reached or returns something unusable.</summary>
public sealed class LlmUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
