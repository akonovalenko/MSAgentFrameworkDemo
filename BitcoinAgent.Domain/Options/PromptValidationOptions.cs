namespace BitcoinAgent.Domain.Models.Options
{
    /// <summary>
    /// Options for validating prompts before sending them to the AI model.
    /// </summary>
    public sealed class PromptValidationOptions
    {
        public const string SectionName = "PromptValidation";

        public int MaxPromptLength { get; init; } = 4000;
        public int MaxControlCharacters { get; init; } = 20;
        public int MaxLines { get; init; } = 200;
        public bool RejectBinaryInput { get; init; } = true;
    }
}
