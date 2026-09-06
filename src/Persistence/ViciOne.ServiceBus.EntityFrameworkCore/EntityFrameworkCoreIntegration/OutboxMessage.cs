using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides an outbox message implementation.
/// </summary>
public class OutboxMessage :
    OutboxMessageContext
{
    Headers? _headers;
    IReadOnlyDictionary<string, object>? _properties;

    /// <summary>
    /// When the message should be visible / ready to be delivered
    /// </summary>
    public DateTimeOffset? EnqueueTime { get; set; }

    /// <summary>
    /// Gets or sets the sent time value.
    /// </summary>
    public DateTimeOffset SentTime { get; set; }

    /// <summary>
    /// Gets or sets the headers value.
    /// </summary>
    public string? Headers { get; set; }

    /// <summary>
    /// Transport-specific message properties (routing key, partition key, sessionId, etc.)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Used for inbox + outbox only messages, which are by consumer
    /// </summary>
    public Guid? InboxMessageId { get; set; }

    /// <summary>
    /// Gets or sets the inbox consumer id value.
    /// </summary>
    public Guid? InboxConsumerId { get; set; }

    /// <summary>
    /// Used for outbox (on-ramp) only messages, which are on a separate index
    /// </summary>
    public Guid? OutboxId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number value.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the message id value.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the content type value.
    /// </summary>
    public string ContentType { get; set; } = null!;
    /// <summary>
    /// Gets or sets the message type value.
    /// </summary>
    public string MessageType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the body value.
    /// </summary>
    public string Body { get; set; } = null!;

    /// <summary>
    /// Gets or sets the conversation id value.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the initiator id value.
    /// </summary>
    public Guid? InitiatorId { get; set; }

    /// <summary>
    /// Gets or sets the request id value.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the source address value.
    /// </summary>
    public Uri? SourceAddress { get; set; }
    /// <summary>
    /// Gets or sets the destination address value.
    /// </summary>
    public Uri? DestinationAddress { get; set; }
    /// <summary>
    /// Gets or sets the response address value.
    /// </summary>
    public Uri? ResponseAddress { get; set; }
    /// <summary>
    /// Gets or sets the fault address value.
    /// </summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>
    /// If the message is not delivered to the transport within the expiration time, consider moving to a dead-letter queue instead
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    Guid? MessageContext.MessageId => MessageId;
    DateTimeOffset? MessageContext.SentTime => SentTime;
    Headers MessageContext.Headers => _headers ?? EmptyHeaders.Instance;
    HostInfo MessageContext.Host => HostMetadataCache.Host;

    IReadOnlyDictionary<string, object> OutboxMessageContext.Properties => _properties!;

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="deserializer">The deserializer value.</param>
    public void Deserialize(IObjectDeserializer deserializer)
    {
        _headers = DeserializerHeaders(deserializer);
        _properties = DeserializerProperties(deserializer);
    }

    Headers DeserializerHeaders(IObjectDeserializer deserializer)
    {
        Dictionary<string, object?>? headers = deserializer.DeserializeDictionary<object?>(Headers);
        if (headers != null)
            return new DictionarySendHeaders(headers);

        return EmptyHeaders.Instance;
    }

    IReadOnlyDictionary<string, object> DeserializerProperties(IObjectDeserializer deserializer)
    {
        Dictionary<string, object>? properties = deserializer.DeserializeDictionary<object>(Properties);

        return properties ?? OutboxMessageStaticData.Empty;
    }
}


static class OutboxMessageStaticData
{
    public static IReadOnlyDictionary<string, object> Empty { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
