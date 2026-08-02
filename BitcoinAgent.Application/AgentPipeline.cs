using BitcoinAgent.Application.Interfaces;

namespace BitcoinAgent.Application;

/// <summary>
/// Represents a pipeline of ordered middleware components that can be executed in sequence.
/// </summary>
public sealed class AgentPipeline
{
    private readonly IReadOnlyList<IOrderedMiddleware> _middlewares;

    public AgentPipeline(IEnumerable<IOrderedMiddleware> middlewares)
    {
        this._middlewares = middlewares
                .OrderBy(x => x.Order)
                .ToList();
    }

    /// <summary>
    /// Executes the middleware pipeline with the given context and terminal delegate.
    /// </summary>
    /// <param name="context">The context for the middleware pipeline.</param>
    /// <param name="terminal">The terminal delegate for the middleware pipeline.</param>
    /// <param name="cancellationToken">The cancellation token for the middleware pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExecuteAsync(
        AgentContext context,
        AgentDelegate terminal,
        CancellationToken cancellationToken)
    {
        AgentDelegate pipeline = terminal;

        foreach (var middleware in _middlewares.Reverse())
        {
            var next = pipeline;

            pipeline = (ctx, ct) =>
                middleware.InvokeAsync(
                    ctx,
                    next,
                    ct);
        }

        return pipeline(context, cancellationToken);
    }

}