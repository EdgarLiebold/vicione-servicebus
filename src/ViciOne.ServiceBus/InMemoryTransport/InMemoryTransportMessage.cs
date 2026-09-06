using System;
using System.Threading;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory transport message implementation.
/// </summary>
public class InMemoryTransportMessage
{
    static long _nextSequenceNumber;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageId">The message id value.</param>
    /// <param name="body">The body value.</param>
    /// <param name="contentType">The content type value.</param>
    public InMemoryTransportMessage(Guid messageId, byte[] body, string? contentType)
    {
        Headers = new DictionarySendHeaders();
        MessageId = messageId;
        Body = body;

        Headers.Set(MessageHeaders.MessageId, messageId.ToString());
        Headers.Set(MessageHeaders.ContentType, contentType);

        SequenceNumber = Interlocked.Increment(ref _nextSequenceNumber);
    }

    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    public long SequenceNumber { get; }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public byte[] Body { get; }

    /// <summary>
    /// Gets or sets the delivery count value.
    /// </summary>
    public int DeliveryCount { get; set; }

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public SendHeaders Headers { get; }

    /// <summary>
    /// Gets or sets the delay value.
    /// </summary>
    public TimeSpan? Delay { get; set; }
    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string? RoutingKey { get; set; }

    internal InMemoryDurableSendContext? DurableSendContext { get; set; }
}
