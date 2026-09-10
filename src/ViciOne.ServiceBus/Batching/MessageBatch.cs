using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Provides the immutable ordered snapshot delivered through the public batch contract.</summary>
/// <typeparam name="TMessage">The message contract contained in the snapshot.</typeparam>
internal sealed class MessageBatch<TMessage> :
    Batch<TMessage>
    where TMessage : class
{
    readonly IReadOnlyList<ConsumeContext<TMessage>> _messages;

    /// <summary>Creates a detached snapshot with its completion metadata.</summary>
    /// <param name="firstMessageReceived">When collection for the batch began.</param>
    /// <param name="lastMessageReceived">When the most recent included message arrived.</param>
    /// <param name="mode">The condition that completed the batch.</param>
    /// <param name="messages">The ordered message contexts included in the batch.</param>
    public MessageBatch(DateTimeOffset firstMessageReceived, DateTimeOffset lastMessageReceived, BatchCompletionMode mode,
        IReadOnlyList<ConsumeContext<TMessage>> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        FirstMessageReceived = firstMessageReceived;
        LastMessageReceived = lastMessageReceived;
        Mode = mode;
        _messages = [.. messages];
    }

    /// <summary>Gets the condition that completed the batch.</summary>
    public BatchCompletionMode Mode { get; }
    /// <summary>Gets when collection for the batch began.</summary>
    public DateTimeOffset FirstMessageReceived { get; }
    /// <summary>Gets when the most recent included message arrived.</summary>
    public DateTimeOffset LastMessageReceived { get; }

    /// <summary>Gets the message context at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index.</param>
    public ConsumeContext<TMessage> this[int index] => _messages[index];

    /// <summary>Gets the number of message contexts in the snapshot.</summary>
    public int Length => _messages.Count;

    /// <summary>Returns an enumerator over the ordered message contexts.</summary>
    /// <returns>An enumerator over the snapshot.</returns>
    public IEnumerator<ConsumeContext<TMessage>> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
