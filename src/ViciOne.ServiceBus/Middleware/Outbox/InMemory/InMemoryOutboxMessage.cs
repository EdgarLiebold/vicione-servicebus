using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Middleware.Outbox.InMemory;

/// <summary>Stores a serialized outgoing message while it awaits durable-outbox delivery.</summary>
internal sealed class InMemoryOutboxMessage :
    OutboxMessageContext
{
    Headers _deserializedHeaders = EmptyHeaders.Instance;
    IReadOnlyDictionary<string, object> _deserializedProperties = OutboxMessageStaticData.Empty;

    /// <summary>Gets or sets when the transport may make the message visible.</summary>
    public DateTimeOffset? EnqueueTime { get; set; }

    /// <summary>Gets or sets when the original send operation was created.</summary>
    public DateTimeOffset SentTime { get; set; }

    /// <summary>Gets or sets the serialized message headers.</summary>
    public string? Headers { get; set; }

    /// <summary>Gets or sets serialized transport-specific properties such as routing or partition keys.</summary>
    public string? Properties { get; set; }

    /// <summary>Gets or sets the one-based delivery sequence within the inbox entry.</summary>
    public long SequenceNumber { get; set; }

    /// <summary>Gets or sets the outgoing message identifier.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the serialized body content type.</summary>
    public required string ContentType { get; set; }
    /// <summary>Gets or sets the semicolon-delimited message-type URNs.</summary>
    public required string MessageType { get; set; }
    /// <summary>Gets or sets the serialized message body.</summary>
    public required string Body { get; set; }

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
    Headers MessageContext.Headers => _deserializedHeaders;
    HostInfo MessageContext.Host => HostMetadataCache.Host;

    IReadOnlyDictionary<string, object> OutboxMessageContext.Properties => _deserializedProperties;

    /// <summary>Materializes the serialized headers and transport properties for delivery.</summary>
    /// <param name="deserializer">The serializer-compatible object deserializer.</param>
    public void Deserialize(IObjectDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(deserializer);
        _deserializedHeaders = DeserializeHeaders(deserializer);
        _deserializedProperties = DeserializeProperties(deserializer);
    }

    Headers DeserializeHeaders(IObjectDeserializer deserializer)
    {
        Dictionary<string, object?>? headers = deserializer.DeserializeDictionary<object?>(Headers);
        if (headers != null)
        {
            return new DictionarySendHeaders(headers
                .Where(static pair => pair.Value is not null)
                .Select(static pair => new KeyValuePair<string, object>(pair.Key, pair.Value!)));
        }

        return EmptyHeaders.Instance;
    }

    IReadOnlyDictionary<string, object> DeserializeProperties(IObjectDeserializer deserializer)
    {
        Dictionary<string, object>? properties = deserializer.DeserializeDictionary<object>(Properties);

        return properties ?? OutboxMessageStaticData.Empty;
    }
}


static class OutboxMessageStaticData
{
    public static IReadOnlyDictionary<string, object> Empty { get; } =
        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
}
