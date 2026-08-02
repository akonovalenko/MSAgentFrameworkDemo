using BitcoinAgent.Application;
using BitcoinAgent.Infrastructure;

namespace BitcoinAgent.Api;

/// <summary>
/// Provides extension methods for registering API services in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Application
        services.AddApplication();

        // Infrastructure
        services.AddInfrastructure(configuration);

        // API
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}