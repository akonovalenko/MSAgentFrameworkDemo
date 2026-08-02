using BitcoinAgent.Application.Interfaces;
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
    private readonly IConversationMemory _memory;

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
    /// <param name="memory">The conversation memory.</param>
    /// <param name="bitcoinTool">The Bitcoin tool.</param>
    public BitcoinAgentHandler(
        IChatClient chatClient,
        IConversationMemory memory,
        IBitcoinTool bitcoinTool)
    {
        this._chatClient = chatClient;
        this._memory = memory;
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

        // Load history BEFORE saving current message
        var history = await _memory.GetRecentMessagesAsync(20, cancellationToken);

        // Save current user message
        await _memory.AddUserMessageAsync(context.Prompt, cancellationToken);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, context.Prompt)
        };

        messages.AddRange(history);
        messages.Add(new ChatMessage(ChatRole.User, context.Prompt));

        // Register the tool
        var getBitcoinPriceFunction = AIFunctionFactory.Create(
            this._bitcoinTool.GetCurrentPriceAsync,
            name: "GetCurrentBitcoinPrice",
            description: "Returns the current Bitcoin price in USD.");

        var options = new ChatOptions
        {
            Temperature = 0.2f,
            Tools = [getBitcoinPriceFunction]
        };

        // ===== First request =====
        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        if (response.Usage is not null)
        {
            context.Items[AgentContextKeys.TokenUsage] = response.Usage;
        }

        // Check if the model requested a tool
        var functionCall = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .FirstOrDefault();

        // If the tool is not needed, we return a regular response
        if (functionCall is null)
        {
            context.Response = ExtractText(response) ?? "Unable to generate response.";
            await _memory.AddAssistantMessageAsync(context.Response, cancellationToken);
            return;
        }

        object? toolResult;
        // ===== run tool =====
        switch (functionCall.Name)
        {
            case "GetCurrentBitcoinPrice":
                toolResult = await _bitcoinTool.GetCurrentPriceAsync(cancellationToken);
                context.Items[AgentContextKeys.BitcoinPriceToolResult] = toolResult;
                break;
            default:
                // The model requested an unknown tool.We don't crash, but return a user-friendly message.
                context.Response = $"Sorry, I cannot perform the requested action because the tool {functionCall.Name} is not available.";
                return;
        }

        messages.Add(new ChatMessage(ChatRole.Assistant, [functionCall]));
        messages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(functionCall.CallId, toolResult)]));

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

        context.Response = ExtractText(finalResponse) ?? toolResult?.ToString() ?? "Tool execution completed.";
        await _memory.AddAssistantMessageAsync(context.Response, cancellationToken);
    }

    /// <summary>
    /// Extracts the text content from a ChatResponse, concatenating all text parts if necessary.
    /// </summary>
    /// <param name="response">The chat response.</param>
    /// <returns>The extracted text.    </returns>
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