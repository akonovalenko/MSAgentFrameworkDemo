using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain.Models;
using Microsoft.Extensions.AI;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Logs token usage reported by the LLM provider.
/// </summary>
public sealed class TokenUsageMiddleware : IOrderedMiddleware
{
    private readonly ILogger<TokenUsageMiddleware> _logger;

    public int Order => (int)MiddlewareOrder.TokenUsage;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenUsageMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TokenUsageMiddleware(
        ILogger<TokenUsageMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Logs token usage reported by the LLM provider.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        await next(context, cancellationToken);

        if (!context.Items.TryGetValue(
                AgentContextKeys.TokenUsage,
                out var value))
        {
            this._logger.LogDebug("Token usage is unavailable.");

            return;
        }

        if (value is not UsageDetails usage)
        {
            this._logger.LogWarning("Invalid token usage object.");

            return;
        }

        this._logger.LogInformation(
            """
            Token usage:
                Input: {InputTokens}
                Output: {OutputTokens}
                Total: {TotalTokens}
            """,
            usage.InputTokenCount,
            usage.OutputTokenCount,
            usage.TotalTokenCount);
    }
}