namespace BitcoinAgent.Domain.Models;

/// <summary>
/// Contains keys used in the agent context for storing and retrieving values.
/// </summary>
public static class AgentContextKeys
{
    public const string BitcoinPriceToolResult = nameof(BitcoinPriceToolResult);
    public const string RetryAttempt = nameof(RetryAttempt);
    public const string Exception = nameof(Exception);
    public const string ExceptionTimestamp = nameof(ExceptionTimestamp);
    public const string TokenUsage = nameof(TokenUsage);
    public const string RetryRequired = nameof(RetryRequired);
    public const string RetryReason = nameof(RetryReason);
    public const string CorrelationId = nameof(CorrelationId);
}