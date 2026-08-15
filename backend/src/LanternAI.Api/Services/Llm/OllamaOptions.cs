namespace LanternAI.Api.Services.Llm;

/// <summary>
/// Bound from the "Ollama" configuration section. Override via environment
/// variables (Ollama__BaseUrl, Ollama__Model) rather than editing
/// appsettings.json for a given environment — see README.
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "qwen2.5-coder";

    /// <summary>Request timeout; local model inference can be slow on first load.</summary>
    public int TimeoutSeconds { get; set; } = 60;
}
