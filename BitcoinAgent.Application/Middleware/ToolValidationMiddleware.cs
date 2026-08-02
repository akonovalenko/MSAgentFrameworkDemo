using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Application.Validators;
using BitcoinAgent.Domain.Models;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Middleware that validates the output of a tool (e.g., BitcoinPrice) after it has been executed.
/// </summary>
public sealed class ToolValidationMiddleware : IOrderedMiddleware
{
    private readonly BitcoinResponseValidator _validator;
    private readonly ILogger<ToolValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolValidationMiddleware"/> class.
    /// </summary>
    /// <param name="validator">The validator to use for validating tool output.</param>
    /// <param name="logger">The logger to use for logging validation messages.</param>
    public ToolValidationMiddleware(
        BitcoinResponseValidator validator,
        ILogger<ToolValidationMiddleware> logger)
    {
        this._validator = validator;
        this._logger = logger;
    }

    public int Order => (int)MiddlewareOrder.ToolValidation;

    /// <summary>
    /// Invokes the middleware to validate the output of a tool after it has been executed.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tool output is invalid.</exception>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        await next(context, cancellationToken);

        // Handler must store BitcoinPrice in the context.
        if (!context.Items.TryGetValue(
                AgentContextKeys.BitcoinPriceToolResult,
                out var value) ||
            value is not BitcoinPrice response)
        {
            this._logger.LogDebug(
                "Tool validation skipped because '{Key}' was not found in AgentContext.", AgentContextKeys.BitcoinPriceToolResult);

            return;
        }

        var validation = this._validator.Validate(response);

        if (validation.IsValid)
        {
            this._logger.LogInformation("Bitcoin tool output validation succeeded.");

            return;
        }

        foreach (var error in validation.Errors)
        {
            this._logger.LogError("Bitcoin tool validation error: {Error}", error);
        }

        throw new InvalidOperationException(
            $"Bitcoin tool output validation failed. Errors: {string.Join("; ", validation.Errors)}");
    }
}