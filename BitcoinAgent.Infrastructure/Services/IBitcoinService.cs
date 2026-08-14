using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitcoinAgent.Infrastructure.Services
{
    /// <summary>
    /// Abstraction for retrieving Bitcoin prices from an external provider (CoinGecko).
    /// </summary>
    public interface IBitcoinService
    {
        /// <summary>
        /// Returns current Bitcoin price in USD.
        /// </summary>
        Task<decimal> GetBitcoinPriceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns historical Bitcoin price in USD for the specified date.
        /// </summary>
        Task<decimal> GetHistoricalBitcoinPriceAsync(DateOnly date, CancellationToken cancellationToken = default);
    }
}
