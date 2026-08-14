using System.ComponentModel;
using BitcoinAgent.Domain;
using BitcoinAgent.Domain.Models;
using BitcoinAgent.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace BitcoinAgent.Infrastructure.Tools;

/// <summary>
/// Tool that retrieves the current Bitcoin price from CoinGecko.
/// </summary>
public sealed class BitcoinTool : IBitcoinTool
{
    private readonly IBitcoinService _bitcoinService;
    private readonly ILogger<BitcoinTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitcoinTool"/> class.
    /// </summary>
    /// <param name="bitcoinService">Service used to retrieve Bitcoin prices.</param>
    /// <param name="logger">Logger instance.</param>
    public BitcoinTool(
        IBitcoinService bitcoinService,
        ILogger<BitcoinTool> logger)
    {
        this._bitcoinService = bitcoinService;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current Bitcoin price in USD from CoinGecko.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current Bitcoin price information.</returns>
    [Description("Gets the current Bitcoin price in USD from CoinGecko.")]
    public async Task<BitcoinPrice> GetCurrentPriceAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Log tool execution start.
        this._logger.LogInformation("Executing Bitcoin price tool.");

        try
        {
            // Retrieve the latest Bitcoin price from CoinGecko.
            var price = await this._bitcoinService.GetBitcoinPriceAsync(cancellationToken);

            // Validate the returned price.
            if (price <= 0)
            {
                throw new InvalidOperationException($"Invalid Bitcoin price returned: {price}");
            }

            // Build the domain response model.
            var result = new BitcoinPrice
            {
                Symbol = "BTC",
                Currency = "USD",
                Price = price,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Log successful retrieval.
            this._logger.LogInformation("Bitcoin price retrieved: {Price} {Currency}", result.Price, result.Currency);

            return result;
        }
        catch (OperationCanceledException)
        {
            this._logger.LogWarning("BitcoinTool execution cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            // Log unexpected failures and rethrow.
            this._logger.LogError(ex, "BitcoinTool execution failed.");
            throw;
        }
    }


    /// <summary>
    /// Gets the historical Bitcoin price in USD for a specific date from CoinGecko.
    /// </summary>
    /// <param name="date">Requested historical date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical Bitcoin price information.</returns>
    [Description("Gets the Bitcoin price in USD for a specific historical date from CoinGecko.")]
    public async Task<BitcoinPrice> GetHistoricalPriceAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Log tool execution start.
        this._logger.LogInformation("Executing historical Bitcoin price tool for date {Date}.", date);

        try
        {
            // Retrieve historical Bitcoin price from CoinGecko.
            var price = await this._bitcoinService.GetHistoricalBitcoinPriceAsync(date, cancellationToken);

            // Validate the returned price.
            if (price <= 0)
            {
                throw new InvalidOperationException($"Invalid historical Bitcoin price returned: {price}");
            }

            // Build the domain response model.
            var result = new BitcoinPrice
            {
                Symbol = "BTC",
                Currency = "USD",
                Price = price,
                Timestamp = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            };

            // Log successful retrieval.
            this._logger.LogInformation("Historical Bitcoin price retrieved for {Date}: {Price} {Currency}", date, result.Price, result.Currency);

            return result;
        }
        catch (OperationCanceledException)
        {
            this._logger.LogWarning("Historical Bitcoin price tool execution cancelled for date {Date}.",date);
            throw;
        }
        catch (Exception ex)
        {
            // Log unexpected failures and rethrow.
            this._logger.LogError(ex, "Historical Bitcoin price tool execution failed for date {Date}.", date);
            throw;
        }
    }

}