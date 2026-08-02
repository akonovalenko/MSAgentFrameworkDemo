using Microsoft.Extensions.AI;

namespace BitcoinAgent.Application.Interfaces;

/// <summary>
/// Stores conversation history for a user/session.
/// The first implementation is in-memory,
/// but later it can be replaced with Redis,
/// Cosmos DB, PostgreSQL, etc.
/// </summary>
public interface IConversationMemory
{
    /// <summary>
    /// Adds a user message to the conversation history.
    /// </summary>
    /// <param name="content">The content of the user message.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddUserMessageAsync(string content, CancellationToken ct = default);
    
    /// <summary>
    /// Adds an assistant message to the conversation history.
    /// </summary>
    /// <param name="content">The content of the assistant message.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAssistantMessageAsync(string content, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent messages from the conversation history.
    /// </summary>
    /// <param name="count">The number of messages to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(int count, CancellationToken ct = default);
    
    /// <summary>
    /// Clears the conversation history.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync(CancellationToken ct = default);
}