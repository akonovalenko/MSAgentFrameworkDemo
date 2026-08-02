namespace BitcoinAgent.Domain.Models;

/// <summary>
/// Exception thrown when a user exceeds the configured rate limit.
/// </summary>
public sealed class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message)
        : base(message)
    {
    }
}