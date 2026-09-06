using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Groups message values into a batch.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageBatch<TMessage> :
    Batch<TMessage>
    where TMessage : class
{
    readonly IReadOnlyList<ConsumeContext<TMessage>> _messages;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="firstMessageReceived">The first message received.</param>
    /// <param name="lastMessageReceived">The last message received.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="messages">The messages.</param>
    public MessageBatch(DateTimeOffset firstMessageReceived, DateTimeOffset lastMessageReceived, BatchCompletionMode mode,
        IReadOnlyList<ConsumeContext<TMessage>> messages)
    {
        FirstMessageReceived = firstMessageReceived;
        LastMessageReceived = lastMessageReceived;
        Mode = mode;
        _messages = messages;
    }

    /// <summary>Gets or sets the mode.</summary>
    public BatchCompletionMode Mode { get; set; }
    /// <summary>Gets or sets the first message received.</summary>
    public DateTimeOffset FirstMessageReceived { get; set; }
    /// <summary>Gets or sets the last message received.</summary>
    public DateTimeOffset LastMessageReceived { get; set; }

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="index">The index.</param>
    public ConsumeContext<TMessage> this[int index] => _messages[index];

    /// <summary>Gets the length.</summary>
    public int Length => _messages.Count;

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<ConsumeContext<TMessage>> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
