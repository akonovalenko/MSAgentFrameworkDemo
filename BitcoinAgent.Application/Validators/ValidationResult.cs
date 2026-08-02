namespace BitcoinAgent.Application.Validators;

/// <summary>
/// Represents the result of output validation.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; }

    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Indicates whether the validation was successful.</param>
    /// <param name="errors">The list of errors.</param>
    private ValidationResult(
        bool isValid,
        IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>The successful validation result.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult(true, Array.Empty<string>());
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>The failed validation result.</returns>
    public static ValidationResult Failed(IEnumerable<string> errors)
    {
        return new ValidationResult(false, errors.ToArray());
    }
}