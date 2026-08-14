using BitcoinAgent.Domain;
using BitcoinAgent.Domain.Models;
using Microsoft.Extensions.AI;

namespace BitcoinAgent.Application;

/// <summary>
/// Handles the logic for interacting with the chat client and Bitcoin tool, processing user prompts, and generating appropriate responses based on the context and tool results.
/// </summary>
public sealed class BitcoinAgentHandler
{
    private readonly IChatClient _chatClient;
    private readonly IBitcoinTool _bitcoinTool;

    private const string SystemPrompt =
        """
        You are a general AI assistant.

        Rules:
        - Answer in the user's language.
        - Use the Bitcoin tool only when the user explicitly asks about Bitcoin price.
        - Never invent Bitcoin prices.
        - For non-Bitcoin questions, answer normally without tools.
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitcoinAgentHandler"/> class with the specified chat client and Bitcoin tool.
    /// </summary>
    /// <param name="chatClient">The chat client.</param>
    /// <param name="bitcoinTool">The Bitcoin tool.</param>
    public BitcoinAgentHandler(
        IChatClient chatClient,
        IBitcoinTool bitcoinTool)
    {
        this._chatClient = chatClient;
        this._bitcoinTool = bitcoinTool;
    }

    /// <summary>
    /// Executes the agent handler logic, processing the user's prompt and interacting with the chat client and Bitcoin tool as needed.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
     AgentContext context,
     CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt)
        };

        messages.AddRange(context.History);
        messages.Add(new ChatMessage(ChatRole.User, context.Prompt));

        // Register the current price tool.
        var getBitcoinPriceFunction = AIFunctionFactory.Create(
            this._bitcoinTool.GetCurrentPriceAsync,
            name: "GetCurrentBitcoinPrice",
            description: "Returns the current Bitcoin price in USD.");

        // Register the historical price tool.
        var getHistoricalBitcoinPriceFunction = AIFunctionFactory.Create(
            this._bitcoinTool.GetHistoricalPriceAsync,
            name: "GetHistoricalBitcoinPrice",
            description: "Returns the Bitcoin price in USD for a specific date.");

        var options = new ChatOptions
        {
            Temperature = 0.2f,
            Tools =
            [
                getBitcoinPriceFunction,
                getHistoricalBitcoinPriceFunction
            ]
        };

        // ===== First request =====
        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        if (response.Usage is not null)
        {
            context.Items[AgentContextKeys.TokenUsage] = response.Usage;
        }

        // Check if the model requested a tool.
        var functionCall = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .FirstOrDefault();

        // If the tool is not needed, we return a regular response.
        if (functionCall is null)
        {
            context.Response = ExtractText(response) ?? "Unable to generate response.";
            return;
        }

        object? toolResult;

        // ===== Run tool =====
        switch (functionCall.Name)
        {
            case "GetCurrentBitcoinPrice":
                toolResult = await _bitcoinTool.GetCurrentPriceAsync(cancellationToken);
                context.Items[AgentContextKeys.BitcoinPriceToolResult] = toolResult;
                break;

            case "GetHistoricalBitcoinPrice":
                {
                    // Extract the date argument from the function call.
                    if (!functionCall.Arguments.TryGetValue("date", out var dateValue)
                        || dateValue is null)
                    {
                        context.Response =
                            "I need a date to retrieve the historical Bitcoin price.";
                        return;
                    }

                    if (!DateOnly.TryParse(dateValue.ToString(), out var date))
                    {
                        context.Response = "I could not understand the requested date.";
                        return;
                    }

                    toolResult = await _bitcoinTool.GetHistoricalPriceAsync(
                        date,
                        cancellationToken);

                    context.Items[AgentContextKeys.BitcoinPriceToolResult] = toolResult;
                    break;
                }

            default:
                // The model requested an unknown tool. We don't crash,
                // but return a user-friendly message.
                context.Response =$"Sorry, I cannot perform the requested action because the tool {functionCall.Name} is not available.";
                return;
        }

        messages.Add(new ChatMessage(ChatRole.Assistant, [functionCall]));
        messages.Add(
            new ChatMessage(
                ChatRole.Tool,
                [new FunctionResultContent(functionCall.CallId, toolResult)]));

        // The second request to the model is to generate the final response to the user.
        // At this stage, we no longer pass tools, otherwise some models might again
        // enter function-calling mode and return empty text.
        var finalResponse = await _chatClient.GetResponseAsync(
            messages,
            new ChatOptions { Temperature = 0.2f },
            cancellationToken);

        if (finalResponse.Usage is not null)
        {
            context.Items[AgentContextKeys.TokenUsage] = finalResponse.Usage;
        }

        context.Response =
            ExtractText(finalResponse)
            ?? toolResult?.ToString()
            ?? "Tool execution completed.";
    }

    /// <summary>
    /// Extracts the text content from a ChatResponse, concatenating all text parts if necessary.
    /// </summary>
    /// <param name="response">The chat response.</param>
    /// <returns>The extracted text.</returns>
    private static string ExtractText(ChatResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text;
        }

        if (response.Messages is null)
        {
            return string.Empty;
        }

        var parts = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<TextContent>()
            .Select(c => c.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return string.Join("", parts);
    }
}