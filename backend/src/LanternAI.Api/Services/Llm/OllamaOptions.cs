namespace LanternAI.Api.Services.Llm;

/// <summary>
/// Bound from the "Ollama" configuration section. Override via environment
/// variables (Ollama__BaseUrl, Ollama__Model, Ollama__ApiKey) rather than editing
/// appsettings.json for a given environment — see README.
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "https://ollama.com";

    public string Model { get; set; } = "qwen3-coder:480b";

    public string ApiKey { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 120;
}
