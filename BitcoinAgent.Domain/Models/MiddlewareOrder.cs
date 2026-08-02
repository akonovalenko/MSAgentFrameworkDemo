namespace BitcoinAgent.Domain.Models;

/// <summary>
/// Defines execution order for agent middleware pipeline.
/// Lower value means earlier execution.
/// </summary>
public enum MiddlewareOrder
{
    /// <summary>
    /// Creates CorrelationId for the whole request pipeline.
    /// Must be executed before logging and audit.
    /// </summary>
    Correlation = 100,

    /// <summary>
    /// Limits request frequency before any expensive processing occurs.
    /// </summary>
    RateLimit = 200,

    /// <summary>
    /// Validates incoming prompt and request metadata.
    /// </summary>
    PromptValidation = 300,

    /// <summary>
    /// Logs request and response lifecycle.
    /// Executed after validation to avoid logging invalid payloads.
    /// </summary>
    Logging = 400,

    /// <summary>
    /// Stores request/response audit information.
    /// </summary>
    Audit = 500,

    /// <summary>
    /// Handles unexpected exceptions.
    /// Should wrap most of the pipeline.
    /// </summary>
    Exception = 600,

    /// <summary>
    /// Retries transient failures:
    /// - LLM empty responses,
    /// - tool failures,
    /// - network errors.
    /// </summary>
    Retry = 700,

    /// <summary>
    /// Validates external tool execution results.
    /// </summary>
    ToolValidation = 800,

    /// <summary>
    /// Validates final LLM response.
    /// </summary>
    LlmResponseValidation = 900,

    /// <summary>
    /// Collects execution metrics.
    /// </summary>
    Metrics = 1000,

    /// <summary>
    /// Collects token usage and cost information.
    /// </summary>
    TokenUsage = 1100
}