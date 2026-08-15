namespace LanternAI.Api.Services.Llm;

public sealed record LlmCompletion(string Content, int? PromptTokens = null, int? CompletionTokens = null)
{
    public int? TotalTokens => PromptTokens.HasValue || CompletionTokens.HasValue
        ? (PromptTokens ?? 0) + (CompletionTokens ?? 0)
        : null;
}