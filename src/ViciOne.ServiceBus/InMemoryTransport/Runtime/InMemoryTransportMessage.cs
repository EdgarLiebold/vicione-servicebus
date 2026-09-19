using System;
using System.Threading;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Contains the serialized envelope delivered through the in-memory message fabric.</summary>
internal sealed class InMemoryTransportMessage
{
    static long _nextSequenceNumber;

    /// <summary>Creates a transport envelope with a process-wide sequence number.</summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="contentType">The serialized body's media type.</param>
    public InMemoryTransportMessage(Guid messageId, byte[] body, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(body);

        Headers = new DictionarySendHeaders();
        MessageId = messageId;
        Body = body;

        Headers.Set(MessageHeaders.MessageId, messageId.ToString());
        Headers.Set(MessageHeaders.ContentType, contentType);

        SequenceNumber = Interlocked.Increment(ref _nextSequenceNumber);
    }

    /// <summary>Gets the monotonically increasing process-wide delivery sequence.</summary>
    public long SequenceNumber { get; }

    /// <summary>Gets the message identifier.</summary>
    public Guid MessageId { get; }

    /// <summary>Gets the serialized message body.</summary>
    public byte[] Body { get; }

    /// <summary>Gets or sets the number of failed receive attempts.</summary>
    public int DeliveryCount { get; set; }

    /// <summary>Gets the transport headers.</summary>
    public SendHeaders Headers { get; }

    /// <summary>Gets or sets the relative delivery delay.</summary>
    public TimeSpan? Delay { get; set; }
    /// <summary>Gets or sets the routing key used by direct and topic exchanges.</summary>
    public string? RoutingKey { get; set; }

    internal InMemoryDurableSendContext? DurableSendContext { get; set; }

    internal InMemoryPayloadAdmissionProof? PayloadAdmissionProof { get; set; }
}

/// <summary>Binds an admitted in-memory envelope to the runtime of its originating bus.</summary>
internal sealed record InMemoryPayloadAdmissionProof(IPayloadAdmissionRuntime OwnerRuntime, DurablePayloadAdmissionProof Proof);
