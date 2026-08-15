namespace LanternAI.Api.Services.Llm;

/// <summary>
/// Placeholder for the future Gemini-backed <see cref="ILlmProvider"/>.
/// Not registered in DI yet — Phase 1 runs entirely on <see cref="OllamaLlmProvider"/>.
///
/// To activate: implement CompleteAsync against the Gemini API (system + user
/// prompt in, completion text out — same contract Ollama fulfills), add the
/// API key via configuration/secret store (never hardcoded), and swap the
/// registration in Program.cs from AddOllamaLlmProvider to this class. No
/// other layer (query planning, endpoints, frontend) needs to change, since
/// everything above this class only depends on <see cref="ILlmProvider"/>.
/// </summary>
public sealed class GeminiLlmProvider : ILlmProvider
{
    public Task<LlmCompletion> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Gemini provider is not implemented yet — Phase 1 uses Ollama. See class remarks.");
}
