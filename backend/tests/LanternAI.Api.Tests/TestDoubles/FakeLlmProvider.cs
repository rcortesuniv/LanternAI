using LanternAI.Api.Services.Llm;

namespace LanternAI.Api.Tests.TestDoubles;

/// <summary>Returns a canned response instead of calling a real model, so query-planning tests are deterministic and offline.</summary>
public sealed class FakeLlmProvider(string response) : ILlmProvider
{
    public string? LastUserPrompt { get; private set; }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        LastUserPrompt = userPrompt;
        return Task.FromResult(response);
    }
}
