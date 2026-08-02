namespace BitcoinAgent.Domain.Models
{
    /// <summary>
    /// Exception thrown when a prompt validation fails.
    /// </summary>
    public sealed class PromptValidationException : Exception
    {
        public PromptValidationException(string message)
            : base(message)
        {
        }
    }
}
