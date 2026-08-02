namespace BitcoinAgent.Domain.Models.Options;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5";

    public double Temperature { get; set; } = 0.2;

    public int MaxOutputTokens { get; set; } = 2048;
}