using System;
using System.Threading;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Carries in memory transport message data.</summary>
public class InMemoryTransportMessage
{
    static long _nextSequenceNumber;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageId">The message id.</param>
    /// <param name="body">The body.</param>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    public InMemoryTransportMessage(Guid messageId, byte[] body, string? contentType)
    {
        Headers = new DictionarySendHeaders();
        MessageId = messageId;
        Body = body;

        Headers.Set(MessageHeaders.MessageId, messageId.ToString());
        Headers.Set(MessageHeaders.ContentType, contentType);

        SequenceNumber = Interlocked.Increment(ref _nextSequenceNumber);
    }

    /// <summary>Gets the sequence number.</summary>
    public long SequenceNumber { get; }

    /// <summary>Gets the message id.</summary>
    public Guid MessageId { get; }

    /// <summary>Gets the body.</summary>
    public byte[] Body { get; }

    /// <summary>Gets or sets the delivery count.</summary>
    public int DeliveryCount { get; set; }

    /// <summary>Gets the headers.</summary>
    public SendHeaders Headers { get; }

    /// <summary>Gets or sets the delay.</summary>
    public TimeSpan? Delay { get; set; }
    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }

    internal InMemoryDurableSendContext? DurableSendContext { get; set; }
}
