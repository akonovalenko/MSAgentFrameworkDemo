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
    private readonly BitcoinService _bitcoinService;
    private readonly ILogger<BitcoinTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitcoinTool"/> class.
    /// </summary>
    /// <param name="bitcoinService">Service used to retrieve Bitcoin prices.</param>
    /// <param name="logger">Logger instance.</param>
    public BitcoinTool(
        BitcoinService bitcoinService,
        ILogger<BitcoinTool> logger)
    {
        _bitcoinService = bitcoinService;
        _logger = logger;
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
            var price = await _bitcoinService.GetBitcoinPriceAsync(cancellationToken);

            // Validate the returned price.
            if (price <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid Bitcoin price returned: {price}");
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
            this._logger.LogInformation(
                "Bitcoin price retrieved: {Price} {Currency}",
                result.Price,
                result.Currency);

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
}