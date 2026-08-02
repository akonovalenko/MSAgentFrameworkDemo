using BitcoinAgent.Domain.Models;

namespace BitcoinAgent.Domain;

/// <summary>
/// Provides Bitcoin information to the application layer.
/// </summary>
public interface IBitcoinTool
{
    Task<BitcoinPrice> GetCurrentPriceAsync(CancellationToken cancellationToken = default);
}