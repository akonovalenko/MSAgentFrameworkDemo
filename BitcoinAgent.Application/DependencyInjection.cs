using BitcoinAgent.Application.Agents;
using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Application.Memory;
using BitcoinAgent.Application.Middleware;
using BitcoinAgent.Application.Validators;

namespace BitcoinAgent.Application;

/// <summary>
/// Provides extension methods for registering application services in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        //
        // Agent
        //
        services.AddTransient<Agents.BitcoinAgent>();
        services.AddTransient<BitcoinAgentFactory>();
        services.AddTransient<BitcoinAgentHandler>();

        //
        // Pipeline
        //
        services.AddScoped<AgentPipeline>();

        //
        // Validators
        //
        services.AddTransient<BitcoinResponseValidator>();
        services.AddTransient<LlmResponseValidator>();

        //
        // Memory
        //
        services.AddScoped<IConversationMemory, InMemoryConversationMemory>();

        //
        // Middleware registrations
        //
        services.AddTransient<IOrderedMiddleware, LoggingMiddleware>();
        services.AddTransient<IOrderedMiddleware, RetryMiddleware>();
        services.AddTransient<IOrderedMiddleware, AuditMiddleware>();
        services.AddTransient<IOrderedMiddleware, CorrelationMiddleware>();
        services.AddTransient<IOrderedMiddleware, ToolValidationMiddleware>();
        services.AddTransient<IOrderedMiddleware, LLMResponseValidationMiddleware>();
        services.AddTransient<IOrderedMiddleware, ExceptionMiddleware>();
        services.AddTransient<IOrderedMiddleware, TokenUsageMiddleware>();
        services.AddTransient<IOrderedMiddleware, RateLimitMiddleware>();
        services.AddTransient<IOrderedMiddleware, PromptValidationMiddleware>();

        return services;
    }
}