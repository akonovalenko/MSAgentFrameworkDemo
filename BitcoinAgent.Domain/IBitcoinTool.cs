using BitcoinAgent.Domain.Models;

namespace BitcoinAgent.Domain;

/// <summary>
/// Provides Bitcoin information to the application layer.
/// </summary>
public interface IBitcoinTool
{
    /// <summary>
    /// Gets the current price of Bitcoin in USD.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current price of Bitcoin.</returns>
    Task<BitcoinPrice> GetCurrentPriceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the historical price of Bitcoin for a specific date.
    /// </summary>
    /// <param name="date">The date for which to get the historical price.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The historical price of Bitcoin for the specified date.</returns>
    Task<BitcoinPrice> GetHistoricalPriceAsync(DateOnly date, CancellationToken cancellationToken = default);
}