namespace BitcoinAgent.Application.Interfaces;

/// <summary>
/// Defines the contract for middleware components that can be executed in an agent pipeline.
/// </summary>
public interface IAgentMiddleware
{
    /// <summary>
    /// Invokes the middleware with the given context, next delegate, and cancellation token.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeAsync(AgentContext context, AgentDelegate next, CancellationToken cancellationToken);

}