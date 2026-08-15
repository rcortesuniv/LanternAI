namespace LanternAI.Api.Services.Llm;

/// <summary>
/// Minimal chat-completion boundary between Lantern AI and whichever model
/// is actually answering. <see cref="OllamaLlmProvider"/> is the Phase 1
/// implementation (local Ollama); a Gemini-backed implementation can be
/// dropped in later by registering it in place of Ollama in DI — nothing
/// above this interface (query planning, endpoints) needs to change.
/// </summary>
public interface ILlmProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
