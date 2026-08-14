using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BitcoinAgent.Infrastructure.Services;

/// <summary>
/// Service that retrieves the current Bitcoin price from CoinGecko.
/// </summary>
public sealed class BitcoinService : IBitcoinService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitcoinService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitcoinService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to make requests to the CoinGecko API.</param>
    /// <param name="logger">The logger used to log information and errors.</param>
    public BitcoinService(
        HttpClient httpClient,
        ILogger<BitcoinService> logger)
    {
        this._httpClient = httpClient;
        this._logger = logger;
    }

    /// <summary>
    /// Returns current Bitcoin price in USD.
    /// </summary>
    public async Task<decimal> GetBitcoinPriceAsync(
        CancellationToken cancellationToken = default)
    {
        const string endpoint ="simple/price?ids=bitcoin&vs_currencies=usd";

        this._logger.LogInformation("Requesting Bitcoin price from CoinGecko.");

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("bitcoin",out var bitcoin))
        {
            throw new InvalidOperationException("CoinGecko response does not contain 'bitcoin'.");
        }

        if (!bitcoin.TryGetProperty("usd",out var usd))
        {
            throw new InvalidOperationException("CoinGecko response does not contain 'usd'.");
        }

        var price = usd.GetDecimal();

        this._logger.LogInformation("Bitcoin price received: {Price}", price);

        return price;
    }

    /// <summary>
    /// Returns historical Bitcoin price in USD for the specified date.
    /// </summary>
    /// <param name="date">Requested historical date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bitcoin price in USD for the specified date.</returns>
    public async Task<decimal> GetHistoricalBitcoinPriceAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var endpoint =$"coins/bitcoin/history?date={date:dd-MM-yyyy}";

        this._logger.LogInformation("Requesting historical Bitcoin price from CoinGecko for date {Date}.", date);

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("market_data", out var marketData))
        {
            throw new InvalidOperationException("CoinGecko response does not contain 'market_data'.");
        }

        if (!marketData.TryGetProperty("current_price", out var currentPrice))
        {
            throw new InvalidOperationException("CoinGecko response does not contain 'current_price'.");
        }

        if (!currentPrice.TryGetProperty("usd", out var usd))
        {
            throw new InvalidOperationException("CoinGecko response does not contain 'usd'.");
        }

        var price = usd.GetDecimal();

        this._logger.LogInformation("Historical Bitcoin price received for {Date}: {Price}", date, price);

        return price;
    }
}