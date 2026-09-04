using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// The SendContext is used to tweak the send to the endpoint
/// </summary>
/// <typeparam name="T">The message type being sent</typeparam>
public interface SendContext<out T> :
    SendContext
    where T : class
{
    /// <summary>
    /// The message being sent
    /// </summary>
    T Message { get; }
}


/// <summary>
/// The endpoint captures the message before returning this context for higher-level send configuration.
/// </summary>
public interface SendContext :
    PipeContext
{
    /// <summary>
    /// Gets or sets the source address value.
    /// </summary>
    Uri? SourceAddress { get; set; }
    /// <summary>
    /// Gets or sets the destination address value.
    /// </summary>
    Uri? DestinationAddress { get; set; }
    /// <summary>
    /// Gets or sets the response address value.
    /// </summary>
    Uri? ResponseAddress { get; set; }
    /// <summary>
    /// Gets or sets the fault address value.
    /// </summary>
    Uri? FaultAddress { get; set; }

    /// <summary>
    /// Gets or sets the request id value.
    /// </summary>
    Guid? RequestId { get; set; }
    /// <summary>
    /// Gets or sets the message id value.
    /// </summary>
    Guid? MessageId { get; set; }
    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    Guid? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation id value.
    /// </summary>
    Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the initiator id value.
    /// </summary>
    Guid? InitiatorId { get; set; }

    /// <summary>
    /// Gets or sets the scheduled message id value.
    /// </summary>
    Guid? ScheduledMessageId { get; set; }

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    SendHeaders Headers { get; }

    /// <summary>
    /// Gets or sets the time to live value.
    /// </summary>
    TimeSpan? TimeToLive { get; set; }

    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    DateTimeOffset? SentTime { get; }

    /// <summary>
    /// Gets or sets the content type value.
    /// </summary>
    ContentType? ContentType { get; set; }

    /// <summary>
    /// True if the message should be persisted to disk to survive a broker restart
    /// </summary>
    bool Durable { get; set; }

    /// <summary>
    /// If specified, the message delivery will be delayed by the transport (if supported)
    /// </summary>
    TimeSpan? Delay { get; set; }

    /// <summary>
    /// The serializer to use when serializing the message to the transport
    /// </summary>
    IMessageSerializer Serializer { get; set; }

    /// <summary>
    /// The endpoint configured serialization collection
    /// </summary>
    ISerialization Serialization { get; set; }

    /// <summary>
    /// The supported message types for the message being sent/published. For internal use only.
    /// </summary>
    string[] SupportedMessageTypes { get; set; }

    /// <summary>
    /// After serialization, should return the length of the message body
    /// </summary>
    long? BodyLength { get; }

    /// <summary>
    /// Create a send context proxy with the new message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    SendContext<T> CreateProxy<T>(T message)
        where T : class;
}
