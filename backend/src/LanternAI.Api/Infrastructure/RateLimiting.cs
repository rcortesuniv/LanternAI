namespace LanternAI.Api.Infrastructure;

public static class RateLimiting
{
    /// <summary>Applied to POST /api/query — the one endpoint that triggers LLM inference.</summary>
    public const string QueryPolicy = "query";
}
