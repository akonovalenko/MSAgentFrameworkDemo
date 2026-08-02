using BitcoinAgent.Domain.Models;
using BitcoinAgent.Application.Interfaces;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Limits the number of requests per user.
/// </summary>
public sealed class RateLimitMiddleware : IOrderedMiddleware
{
    private static readonly Dictionary<string, List<DateTimeOffset>> Requests = new();

        private static readonly Lock SyncRoot = new();

    /// <summary>
    /// Maximum requests allowed within the time window.
    /// </summary>
    private const int MaxRequests = 10;

    /// <summary>
    /// Sliding time window for rate limiting.
    /// </summary>
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);

    private readonly ILogger<RateLimitMiddleware> _logger;

    public int Order => (int)MiddlewareOrder.RateLimit;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public RateLimitMiddleware(
        ILogger<RateLimitMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to enforce per-user rate limiting.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="RateLimitExceededException">
    /// Thrown when the request limit is exceeded.
    /// </exception>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        var userId = context.UserId == 0
            ? "anonymous"
            : context.UserId.ToString();

        var now = DateTimeOffset.UtcNow;

        lock (SyncRoot)
        {
            if (!Requests.TryGetValue(userId, out var timestamps))
            {
                timestamps = [];
                Requests[userId] = timestamps;
            }

            // Remove timestamps that are outside the sliding window.
            timestamps.RemoveAll(x => now - x > TimeWindow);

            if (timestamps.Count >= MaxRequests)
            {
                this._logger.LogWarning("Rate limit exceeded. User={UserId}", userId);

                throw new RateLimitExceededException($"Rate limit exceeded. Maximum {MaxRequests} requests per minute.");
            }

            timestamps.Add(now);
        }

        // IMPORTANT: do not await inside the lock.
        await next(context, cancellationToken);
    }
}