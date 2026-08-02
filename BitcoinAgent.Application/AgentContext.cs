namespace BitcoinAgent.Application
{
    /// <summary>
    /// Represents the context for an agent's operation, including the prompt, response, cancellation token, correlation ID, and additional items.
    /// </summary>
    public sealed class AgentContext
    {
        public required string Prompt { get; init; }

        public string? Response { get; set; }

        public required CancellationToken CancellationToken { get; init; }

        public string? CorrelationId { get; set; }

        public Dictionary<string, object?> Items { get; } = [];

        public int RetryAttempt { get; set; }

        public long UserId { get; set; }
    }
}
