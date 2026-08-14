using System.ClientModel;
using BitcoinAgent.Domain;
using BitcoinAgent.Domain.Models.Options;
using BitcoinAgent.Domain.Options;
using BitcoinAgent.Infrastructure.Services;
using BitcoinAgent.Infrastructure.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace BitcoinAgent.Infrastructure;

/// <summary>
/// Provides extension methods for registering infrastructure services in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration to use.</param>
    /// <returns>The <see cref="IServiceCollection"/> with the added services.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure options
        services.Configure<CoinGeckoOptions>(configuration.GetSection(CoinGeckoOptions.SectionName));
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));
        services.Configure<PromptValidationOptions>(configuration.GetSection(PromptValidationOptions.SectionName));

        // Configure CoinGecko HTTP client and register IBitcoinService
        services.AddHttpClient<IBitcoinService, BitcoinService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BitcoinAgent");
        });

        // Configure OpenAI chat client
        services.AddSingleton<IChatClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OpenAIOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException("OpenAI API key is missing.");
            }

            var client = new OpenAIClient(
                new ApiKeyCredential(options.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(options.Endpoint)
                });

            return client.GetChatClient(options.Model).AsIChatClient();
        });

        // Register tools
        services.AddTransient<IBitcoinTool, BitcoinTool>();

        return services;
    }
}