namespace BitcoinAgent.Application.Agents;

/// <summary>
/// Factory class for creating instances of <see cref="BitcoinAgent"/> with scoped dependencies.
/// </summary>
public sealed class BitcoinAgentFactory
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BitcoinAgentFactory(IServiceScopeFactory scopeFactory)
    {
        this._scopeFactory = scopeFactory;
    }

    public BitcoinAgent Create()
    {
        var scope = this._scopeFactory.CreateScope();

        return scope.ServiceProvider.GetRequiredService<BitcoinAgent>();
    }
}