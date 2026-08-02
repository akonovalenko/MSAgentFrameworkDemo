namespace BitcoinAgent.Domain.Options;

public sealed class CoinGeckoOptions
{
    public const string SectionName = "CoinGecko";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}