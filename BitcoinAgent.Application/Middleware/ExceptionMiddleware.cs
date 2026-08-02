using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain.Models;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Middleware that logs unhandled exceptions and enriches the agent context.
/// </summary>
public sealed class ExceptionMiddleware : IOrderedMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public int Order =>
        (int)MiddlewareOrder.Exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ExceptionMiddleware(
        ILogger<ExceptionMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        try
        {
            await next(context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            this._logger.LogInformation("Agent request was cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            //
            // Store exception in context for diagnostics.
            //
            context.Items[AgentContextKeys.Exception] = ex;
            context.Items[AgentContextKeys.ExceptionTimestamp] = DateTimeOffset.UtcNow;

            this._logger.LogError(
                ex,
                "Unhandled exception during agent execution., PromptLength={PromptLength}",
                context.Prompt?.Length ?? 0);

            //
            // IMPORTANT:
            // Never swallow the exception.
            // Let ASP.NET Core UseExceptionHandler convert it
            // into a proper HTTP 500 response.
            //
            throw;
        }
    }
}