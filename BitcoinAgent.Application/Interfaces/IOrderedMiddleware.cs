namespace BitcoinAgent.Application.Interfaces;

/// <summary>
/// Represents a middleware component that can be ordered within a pipeline.
/// </summary>
public interface IOrderedMiddleware : IAgentMiddleware
{
    int Order { get; }
}
