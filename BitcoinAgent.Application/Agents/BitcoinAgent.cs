using BitcoinAgent.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace BitcoinAgent.Application.Agents;

/// <summary>
/// High-level AI agent facade that orchestrates conversation memory,
/// middleware pipeline execution, and LLM interaction.
/// </summary>
public sealed class BitcoinAgent
{
    private const int RecentHistoryLimit = 20;
    private const int HistoryPageSize = 50;

    private readonly IConversationMemory _memory;
    private readonly AgentPipeline _pipeline;
    private readonly BitcoinAgentHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitcoinAgent"/> class.
    /// </summary>
    /// <param name="memory">Conversation memory implementation.</param>
    /// <param name="pipeline">Middleware pipeline.</param>
    /// <param name="handler">Core agent handler.</param>
    public BitcoinAgent(
        IConversationMemory memory,
        AgentPipeline pipeline,
        BitcoinAgentHandler handler)
    {
        _memory = memory;
        _pipeline = pipeline;
        _handler = handler;
    }

    /// <summary>
    /// Sends a prompt to the agent and returns the generated response.
    /// </summary>
    /// <param name="prompt">User prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Assistant response.</returns>
    public async Task<string> AskAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        // Load recent conversation history before saving the current message.
        var history = await _memory.GetRecentMessagesAsync(
            RecentHistoryLimit,
            cancellationToken);

        // Persist the user message before processing.
        await _memory.AddUserMessageAsync(
            prompt,
            cancellationToken);

        // Create execution context for the pipeline.
        var context = new AgentContext
        {
            Prompt = prompt,
            CancellationToken = cancellationToken,
            History = history,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        // Execute middleware pipeline and handler.
        await _pipeline.ExecuteAsync(
            context,
            _handler.ExecuteAsync,
            cancellationToken);

        // Use a safe fallback if no response was produced.
        var answer = context.Response ?? "Unable to generate response.";

        // Persist the assistant response.
        await _memory.AddAssistantMessageAsync(
            answer,
            cancellationToken);

        return answer;
    }

    /// <summary>
    /// Returns the most recent conversation messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conversation history.</returns>
    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        // Limit history size to avoid returning an unbounded collection.
        return _memory.GetRecentMessagesAsync(
            HistoryPageSize,
            cancellationToken);
    }

    /// <summary>
    /// Clears the conversation history.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task ClearHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        return _memory.ClearAsync(cancellationToken);
    }
}

/*
                     User
                      │
                      ▼
            "Bitcoin price?"
                      │
                      ▼
         ChatClient.GetResponseAsync()
                      │
                      ▼
    ────────────────────────────────────
             The LLM receives:

      • System prompt
      • Conversation history
      • Available tools/functions
    ────────────────────────────────────
                      │
                      ▼
          Analyze the user request
                      │
                      ▼
      "A Bitcoin price tool is required"
                      │
                      ▼
          Return FunctionCallContent
                      │
                      ▼
             Application code
                      │
                      ▼
    IBitcoinTool.GetCurrentPriceAsync()
                      │
                      ▼
              CoinGecko API
                      │
                      ▼
          FunctionResultContent
                      │
                      ▼
          Second LLM invocation
                      │
                      ▼
       Human-readable final answer
*/