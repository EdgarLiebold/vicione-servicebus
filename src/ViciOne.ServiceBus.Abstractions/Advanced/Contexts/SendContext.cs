using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the typed message and transport metadata for an outgoing send.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface SendContext<out TMessage> :
    SendContext
    where TMessage : class
{
    /// <summary>Gets the outgoing message.</summary>
    TMessage Message { get; }
}


/// <summary>Provides transport metadata and serialization control for an outgoing message.</summary>
public interface SendContext :
    PipeContext
{
    /// <summary>Gets or sets the source address.</summary>
    Uri? SourceAddress { get; set; }
    /// <summary>Gets or sets the destination address.</summary>
    Uri? DestinationAddress { get; set; }
    /// <summary>Gets or sets the response address.</summary>
    Uri? ResponseAddress { get; set; }
    /// <summary>Gets or sets the fault address.</summary>
    Uri? FaultAddress { get; set; }

    /// <summary>Gets or sets the request identifier.</summary>
    Guid? RequestId { get; set; }
    /// <summary>Gets or sets the message identifier.</summary>
    Guid? MessageId { get; set; }
    /// <summary>Gets or sets the correlation identifier.</summary>
    Guid? CorrelationId { get; set; }

    /// <summary>Gets or sets the conversation identifier.</summary>
    Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the initiator identifier.</summary>
    Guid? InitiatorId { get; set; }

    /// <summary>Gets or sets the scheduled-message identifier.</summary>
    Guid? ScheduledMessageId { get; set; }

    /// <summary>Gets the mutable outgoing headers.</summary>
    SendHeaders Headers { get; }

    /// <summary>Gets or sets how long the message remains eligible for delivery.</summary>
    TimeSpan? TimeToLive { get; set; }

    /// <summary>Gets the time assigned during serialization.</summary>
    DateTimeOffset? SentTime { get; }

    /// <summary>Gets or sets the content type.</summary>
    ContentType? ContentType { get; set; }

    /// <summary>Gets or sets whether the transport must preserve the message across broker restarts.</summary>
    bool Durable { get; set; }

    /// <summary>Gets or sets the transport delivery delay.</summary>
    TimeSpan? Delay { get; set; }

    /// <summary>Gets or sets the serializer used to produce the transport body.</summary>
    IMessageSerializer Serializer { get; set; }

    /// <summary>Gets or sets the endpoint's serialization configuration.</summary>
    ISerialization Serialization { get; set; }

    /// <summary>Gets or sets the message-type identifiers declared for the outgoing message.</summary>
    string[] SupportedMessageTypes { get; set; }

    /// <summary>Gets the serialized body length when known.</summary>
    long? BodyLength { get; }

    /// <summary>Creates a typed view of this context for a replacement message.</summary>
    /// <typeparam name="TMessage">The replacement message type.</typeparam>
    /// <param name="message">The replacement message.</param>
    /// <returns>A typed context that shares this context's transport metadata.</returns>
    SendContext<TMessage> CreateProxy<TMessage>(TMessage message)
        where TMessage : class;
}
