using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Carries in memory outbox message data.</summary>
public class InMemoryOutboxMessage :
    OutboxMessageContext
{
    Headers? _headers;
    IReadOnlyDictionary<string, object>? _properties;

    /// <summary>When the message should be visible / ready to be delivered.</summary>
    public DateTimeOffset? EnqueueTime { get; set; }

    /// <summary>Gets or sets the sent time.</summary>
    public DateTimeOffset SentTime { get; set; }

    /// <summary>Gets or sets the headers.</summary>
    public string? Headers { get; set; }

    /// <summary>Transport-specific message properties (routing key, partition key, sessionId, etc.).</summary>
    public string? Properties { get; set; }

    /// <summary>Gets or sets the sequence number.</summary>
    public long SequenceNumber { get; set; }

    /// <summary>Gets or sets the message id.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the content type.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>Gets or sets the message type.</summary>
    public string MessageType { get; set; } = null!;
    /// <summary>Gets or sets the body.</summary>
    public string Body { get; set; } = null!;

    /// <summary>Gets or sets the conversation id.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>Gets or sets the initiator id.</summary>
    public Guid? InitiatorId { get; set; }
    /// <summary>Gets or sets the request id.</summary>
    public Guid? RequestId { get; set; }

    /// <summary>Gets or sets the source address.</summary>
    public Uri? SourceAddress { get; set; }
    /// <summary>Gets or sets the destination address.</summary>
    public Uri? DestinationAddress { get; set; }
    /// <summary>Gets or sets the response address.</summary>
    public Uri? ResponseAddress { get; set; }
    /// <summary>Gets or sets the fault address.</summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    Guid? MessageContext.MessageId => MessageId;
    DateTimeOffset? MessageContext.SentTime => SentTime;
    Headers MessageContext.Headers => _headers ?? EmptyHeaders.Instance;
    HostInfo MessageContext.Host => HostMetadataCache.Host;

    IReadOnlyDictionary<string, object> OutboxMessageContext.Properties => _properties!;

    /// <summary>Deserializes the supplied payload.</summary>
    /// <param name="deserializer">The deserializer.</param>
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
