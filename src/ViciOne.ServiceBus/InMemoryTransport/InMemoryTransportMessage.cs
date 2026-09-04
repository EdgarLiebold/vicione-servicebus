using System;
using System.Threading;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemoryTransportMessage
{
    static long _nextSequenceNumber;

    public InMemoryTransportMessage(Guid messageId, byte[] body, string? contentType)
    {
        Headers = new DictionarySendHeaders();
        MessageId = messageId;
        Body = body;

        Headers.Set(MessageHeaders.MessageId, messageId.ToString());
        Headers.Set(MessageHeaders.ContentType, contentType);

        SequenceNumber = Interlocked.Increment(ref _nextSequenceNumber);
    }

    public long SequenceNumber { get; }

    public Guid MessageId { get; }

    public byte[] Body { get; }

    public int DeliveryCount { get; set; }

    public SendHeaders Headers { get; }

    public TimeSpan? Delay { get; set; }
    public string? RoutingKey { get; set; }

    internal InMemoryDurableSendContext? DurableSendContext { get; set; }
}
