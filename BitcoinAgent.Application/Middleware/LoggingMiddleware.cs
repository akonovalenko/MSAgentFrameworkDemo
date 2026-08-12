using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain.Models;
using System.Diagnostics;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Middleware that logs the execution time of the next middleware in the pipeline.
/// </summary>
public sealed class LoggingMiddleware : IOrderedMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public int Order => (int)MiddlewareOrder.Logging;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public LoggingMiddleware(
        ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Invokes the next middleware and logs execution duration.
    /// </summary>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        this._logger.LogInformation("Agent execution started.");

        try
        {
            await next(context, cancellationToken);

            stopwatch.Stop();

            this._logger.LogInformation("Agent execution completed in {Duration} ms.", stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            stopwatch.Stop();
            throw;
        }
    }
}