namespace LanternAI.Api.Models;

/// <summary>A multi-step investigation sequence where each step builds on the previous result.</summary>
public sealed record PromptbookDefinition(
    string Id,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<PromptbookStep> Steps);

/// <summary>A single step in a promptbook. The question is sent to the LLM with the previous step's result as context.</summary>
public sealed record PromptbookStep(
    string Question,
    string Description,
    /// <summary>Optional: only run this step if the previous result had at least this many rows.</summary>
    int? MinRowsToContinue = null,
    /// <summary>Whether to generate a result summary for this step.</summary>
    bool Summarize = true);

/// <summary>Result of executing a single step in a promptbook.</summary>
public sealed record PromptbookStepResult(
    int StepIndex,
    string Question,
    QueryPlan? Plan,
    QueryResult? Result,
    string? GeneratedKql,
    string? Summary,
    int RowCount,
    bool Skipped);

/// <summary>Full result of executing a promptbook.</summary>
public sealed record PromptbookExecutionResult(
    string PromptbookId,
    string PromptbookName,
    IReadOnlyList<PromptbookStepResult> Steps,
    int TotalDurationMs,
    int TotalTokens);
