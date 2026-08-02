namespace BitcoinAgent.Domain.Models;

/// <summary>
/// Current Bitcoin market price.
/// </summary>
public sealed class BitcoinPrice
{
    public string Symbol { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; }
}