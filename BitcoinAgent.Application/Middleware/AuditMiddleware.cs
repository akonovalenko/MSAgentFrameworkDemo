using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain.Models;
using System.Diagnostics;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Middleware that audits the execution of the next middleware in the pipeline,
/// logging request, response and execution metadata.
/// </summary>
public sealed class AuditMiddleware : IOrderedMiddleware
{
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(
        ILogger<AuditMiddleware> logger)
    {
        this._logger = logger;
    }

    public int Order => (int)MiddlewareOrder.Audit;

    /// <summary>
    /// Invokes the next middleware in the pipeline, logging request and response details.
    /// </summary>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        this._logger.LogInformation(
            """
            ==================== AI REQUEST ====================
            Prompt        : {Prompt}
            ====================================================
            """,
            context.Prompt);

        try
        {
            await next(context, cancellationToken);

            stopwatch.Stop();

            this._logger.LogInformation(
                """
                ==================== AI RESPONSE ===================
                Duration      : {Duration} ms
                Success       : true
                ====================================================
                """,
                stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            stopwatch.Stop();

            var errorMessage = "Unknown error";

            if (context.Items.TryGetValue(
                    AgentContextKeys.Exception,
                    out var value) &&
                value is Exception exception)
            {
                errorMessage = exception.Message;
            }

            // Stack trace is logged by ExceptionMiddleware.
            this._logger.LogWarning(
                """
                ==================== AI ERROR ======================
                Duration      : {Duration} ms
                Success       : false
                Error         : {Error}
                ====================================================
                """,
                stopwatch.ElapsedMilliseconds,
                errorMessage);

            throw;
        }
    }
}