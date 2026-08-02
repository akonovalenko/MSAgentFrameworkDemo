using BitcoinAgent.Domain.Models;

namespace BitcoinAgent.Application.Validators;

/// <summary>
/// Validates Bitcoin tool responses before they are returned to the LLM.
/// </summary>
public sealed class BitcoinResponseValidator
{
    /// <summary>
    /// Validates the given <see cref="BitcoinPrice"/> response.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(BitcoinPrice response)
    {
        var errors = new List<string>();

        if (response is null)
        {
            errors.Add("Response is null.");

            return ValidationResult.Failed(errors);
        }

        if (response.Price <= 0)
        {
            errors.Add("Bitcoin price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(response.Symbol))
        {
            errors.Add("Bitcoin symbol is missing.");
        }

        if (string.IsNullOrWhiteSpace(response.Currency))
        {
            errors.Add("Currency is missing.");
        }

        if (response.Timestamp == default)
        {
            errors.Add("Timestamp is missing.");
        }

        if (response.Timestamp > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            errors.Add("Timestamp is in the future.");
        }

        if (response.Price > 10_000_000m)
        {
            errors.Add("Bitcoin price is outside the expected range.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failed(errors);
    }
}