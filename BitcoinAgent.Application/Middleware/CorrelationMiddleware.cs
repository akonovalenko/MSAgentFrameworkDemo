using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain;
using BitcoinAgent.Domain.Models;
using Microsoft.Extensions.Logging;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Ensures that the request has a correlation identifier and creates
/// a dedicated logging scope for the agent pipeline.
/// </summary>
public sealed class CorrelationMiddleware : IOrderedMiddleware
{
    private readonly ILogger<CorrelationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CorrelationMiddleware(
        ILogger<CorrelationMiddleware> logger)
    {
        _logger = logger;
    }

    public int Order => (int)MiddlewareOrder.Correlation;

    /// <summary>
    /// Invokes the middleware and creates a logging scope for the agent pipeline.
    /// </summary>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        // Preserve an existing identifier if it was already assigned
        // by the API layer; otherwise generate a new one.
        context.CorrelationId ??= Guid.NewGuid().ToString("N");

        using (_logger.BeginScope(
            "AgentCorrelationId:{AgentCorrelationId}",
            context.CorrelationId))
        {
            _logger.LogDebug("Agent correlation scope created");

            await next(context, cancellationToken);
        }
    }
}