using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Batching;

/// <summary>
/// Provides a message batch implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
[Serializable]
public class MessageBatch<TMessage> :
    Batch<TMessage>
    where TMessage : class
{
    readonly IReadOnlyList<ConsumeContext<TMessage>> _messages;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="firstMessageReceived">The first message received value.</param>
    /// <param name="lastMessageReceived">The last message received value.</param>
    /// <param name="mode">The mode value.</param>
    /// <param name="messages">The messages value.</param>
    public MessageBatch(DateTimeOffset firstMessageReceived, DateTimeOffset lastMessageReceived, BatchCompletionMode mode,
        IReadOnlyList<ConsumeContext<TMessage>> messages)
    {
        FirstMessageReceived = firstMessageReceived;
        LastMessageReceived = lastMessageReceived;
        Mode = mode;
        _messages = messages;
    }

    /// <summary>
    /// Gets or sets the mode value.
    /// </summary>
    public BatchCompletionMode Mode { get; set; }
    /// <summary>
    /// Gets or sets the first message received value.
    /// </summary>
    public DateTimeOffset FirstMessageReceived { get; set; }
    /// <summary>
    /// Gets or sets the last message received value.
    /// </summary>
    public DateTimeOffset LastMessageReceived { get; set; }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The index value.</param>
    public ConsumeContext<TMessage> this[int index] => _messages[index];

    /// <summary>
    /// Gets the length value.
    /// </summary>
    public int Length => _messages.Count;

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<ConsumeContext<TMessage>> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
