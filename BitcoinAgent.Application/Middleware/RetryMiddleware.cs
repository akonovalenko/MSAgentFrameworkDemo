using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain.Models;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Retries transient failures during agent execution.
/// </summary>
public sealed class RetryMiddleware : IOrderedMiddleware
{
    private readonly ILogger<RetryMiddleware> _logger;

    public int Order => (int)MiddlewareOrder.Retry;

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    private const int MaxAttempts = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public RetryMiddleware(
        ILogger<RetryMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle retries for transient failures.
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
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                context.RetryAttempt = attempt;

                if (attempt > 1)
                {
                    this._logger.LogWarning("Retry attempt {Attempt}.", attempt);
                }

                await next(context, cancellationToken);

                // Retry requested by downstream middleware (e.g. LLMResponseValidationMiddleware).
                if (context.Items.TryGetValue(AgentContextKeys.RetryRequired, out var retryValue) && retryValue is true)
                {
                    var reason = context.Items.TryGetValue(
                            AgentContextKeys.RetryReason,
                            out var reasonValue)
                        ? reasonValue?.ToString()
                        : "Unknown retry reason";

                    this._logger.LogWarning("Retry requested by downstream middleware. Attempt {Attempt}. Reason: {Reason}.", attempt, reason);

                    if (attempt >= MaxAttempts)
                    {
                        this._logger.LogError("Maximum retry attempts reached. Reason: {Reason}.", reason);
                        return;
                    }

                    context.Items.Remove(AgentContextKeys.RetryRequired);
                    context.Items.Remove(AgentContextKeys.RetryReason);

                    await Task.Delay( GetDelay(attempt), cancellationToken);
                    continue;
                }

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex))
            {
                this._logger.LogWarning(ex, "Transient failure on attempt {Attempt}. Retrying...", attempt);
                await Task.Delay( GetDelay(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                this._logger.LogInformation(ex, "Agent execution failed after {Attempt} attempt(s).", attempt);

                throw;
            }
        }
    }

    /// <summary>
    /// Determines whether the exception is transient and can be retried.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><c>true</c> if the exception is transient; otherwise, <c>false</c>.</returns>
    private static bool IsTransient(Exception exception)
    {
        return exception is HttpRequestException || exception is TimeoutException;
    }

    /// <summary>
    /// Gets the delay before the next retry attempt.
    /// </summary>
    /// <param name="attempt">The current attempt number.</param>
    /// <returns>The delay duration.</returns>
    private static TimeSpan GetDelay(int attempt)
    {
        return TimeSpan.FromMilliseconds(250 * attempt);
    }
}