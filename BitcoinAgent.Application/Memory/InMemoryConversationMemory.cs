using BitcoinAgent.Application.Interfaces;
using Microsoft.Extensions.AI;
using System.Threading;

namespace BitcoinAgent.Application.Memory;

/// <summary>
/// Simple in-memory implementation of <see cref="IConversationMemory"/>.
/// The memory is volatile and is lost when the application stops.
/// Intended for demos, testing, and single-user local scenarios.
/// </summary>
public sealed class InMemoryConversationMemory : IConversationMemory
{
    private readonly List<ChatMessage> _messages = [];

    // Protects access to the in-memory collection.
    private readonly Lock _syncRoot = new();

    /// <summary>
    /// Adds a user message to the conversation history.
    /// </summary>
    public Task AddUserMessageAsync(
        string content,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _messages.Add(new ChatMessage(ChatRole.User, content));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds an assistant message to the conversation history.
    /// </summary>
    public Task AddAssistantMessageAsync(
        string content,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _messages.Add(new ChatMessage(ChatRole.Assistant, content));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the most recent conversation messages.
    /// </summary>
    public Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
        int count,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (count <= 0)
        {
            return Task.FromResult<IReadOnlyList<ChatMessage>>([]);
        }

        IReadOnlyList<ChatMessage> result;

        lock (_syncRoot)
        {
            // Create a snapshot to avoid exposing internal state.
            result = _messages.TakeLast(count).ToList();
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Clears the conversation history.
    /// </summary>
    public Task ClearAsync(
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _messages.Clear();
        }

        return Task.CompletedTask;
    }
}