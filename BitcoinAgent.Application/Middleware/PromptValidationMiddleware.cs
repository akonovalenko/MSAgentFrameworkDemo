using BitcoinAgent.Application.Interfaces;
using BitcoinAgent.Domain.Models;
using BitcoinAgent.Domain.Models.Options;

namespace BitcoinAgent.Application.Middleware;

/// <summary>
/// Validates incoming user prompts before they reach the agent.
/// </summary>
public sealed class PromptValidationMiddleware : IOrderedMiddleware
{
    
    private readonly PromptValidationOptions _options;
    private readonly ILogger<PromptValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptValidationMiddleware"/> class.
    /// </summary>
    /// <param name="logger"></param>
    public PromptValidationMiddleware(
        IOptions<PromptValidationOptions> options,
        ILogger<PromptValidationMiddleware> logger)
    {
        this._logger = logger;
        this._options = options.Value;
    }

    // Run early in the pipeline
    public int Order => (int) MiddlewareOrder.PromptValidation;

    /// <summary>
    /// Validates the incoming user prompt for null, empty, or excessive length before passing it to the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The agent context containing the user prompt.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task InvokeAsync(
        AgentContext context,
        AgentDelegate next,
        CancellationToken cancellationToken)
    {
        if (context.Prompt is null)
        {
            throw new PromptValidationException("Prompt is required.");
        }

        var contextPrompt = context.Prompt.Replace("\r\n", "\n").Trim();

        if (context.Prompt.Length == 0)
        {
            throw new PromptValidationException("Prompt is required.");
        }

        if (contextPrompt.Length > _options.MaxPromptLength)
        {
            this._logger.LogWarning(
                "Prompt rejected because it exceeds the maximum allowed length. Length: {Length}, Max: {Max}",
                contextPrompt.Length,
                _options.MaxPromptLength);

            throw new PromptValidationException($"Prompt is too long. Maximum allowed length is {_options.MaxPromptLength} characters.");
        }

        if (_options.RejectBinaryInput && contextPrompt.Any(c => c == '\0'))
        {
            throw new PromptValidationException("Prompt contains invalid binary characters.");
        }

        var controlCount = contextPrompt.Count(char.IsControl);

        if (controlCount > _options.MaxControlCharacters)
        {
            throw new PromptValidationException($"Prompt contains too many control characters ({controlCount}).");
        }

        var lineCount = contextPrompt.Count(c => c == '\n') + 1;

        if (lineCount > _options.MaxLines)
        {
            throw new PromptValidationException($"Prompt contains too many lines ({lineCount}).");
        }

        this._logger.LogDebug(
            "Prompt validation succeeded. Length: {Length}, Lines: {Lines}",
            context.Prompt.Length,
            lineCount);

        await next(context, cancellationToken);
    }
}