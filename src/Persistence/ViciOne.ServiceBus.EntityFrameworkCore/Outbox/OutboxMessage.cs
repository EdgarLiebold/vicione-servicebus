using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Persists a serialized outgoing message associated with an inbox row or transactional outbox row.</summary>
public class OutboxMessage :
    OutboxMessageContext,
    IDurableOutboxMessageContext
{
    Headers _headers = EmptyHeaders.Instance;
    IReadOnlyDictionary<string, object> _properties = FrozenDictionary<string, object>.Empty;
    DurablePayloadAdmissionProof? _admissionProof;

    /// <summary>Gets or sets the UTC time before which the message must not be sent.</summary>
    public DateTimeOffset? EnqueueTime { get; set; }

    /// <summary>Gets or sets the envelope sent timestamp.</summary>
    public DateTimeOffset SentTime { get; set; }

    /// <summary>Gets or sets the serialized envelope headers restored before transport delivery.</summary>
    public string? Headers { get; set; }

    /// <summary>Gets or sets serialized transport-specific message properties.</summary>
    public string? Properties { get; set; }

    /// <summary>Gets or sets the inbox message identifier for receive-side outbox messages.</summary>
    public Guid? InboxMessageId { get; set; }

    /// <summary>Gets or sets the inbox consumer identifier for receive-side outbox messages.</summary>
    public Guid? InboxConsumerId { get; set; }

    /// <summary>Gets or sets the transactional outbox identifier for messages staged outside consumption.</summary>
    public Guid? OutboxId { get; set; }

    /// <summary>Gets or sets the ordering key assigned by the database.</summary>
    public long SequenceNumber { get; set; }

    /// <summary>Gets or sets the identifier written to the outgoing message envelope.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the media type of the serialized message body.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>Gets or sets the serialized list of supported message-type URNs.</summary>
    public string MessageType { get; set; } = null!;

    /// <summary>Gets or sets the serialized outgoing message body.</summary>
    public string Body { get; set; } = null!;

    /// <summary>Gets or sets the conversation identifier forwarded in the outgoing envelope.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the correlation identifier forwarded in the outgoing envelope.</summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>Gets or sets the initiating message identifier forwarded in the outgoing envelope.</summary>
    public Guid? InitiatorId { get; set; }

    /// <summary>Gets or sets the request identifier forwarded in the outgoing envelope.</summary>
    public Guid? RequestId { get; set; }

    /// <summary>Gets or sets the logical source address forwarded in the outgoing envelope.</summary>
    public Uri? SourceAddress { get; set; }
    /// <summary>Gets or sets the destination transport address used for delivery.</summary>
    public Uri? DestinationAddress { get; set; }
    /// <summary>Gets or sets the response endpoint address forwarded in the outgoing envelope.</summary>
    public Uri? ResponseAddress { get; set; }
    /// <summary>Gets or sets the fault endpoint address forwarded in the outgoing envelope.</summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>Gets or sets the envelope expiration time forwarded to the destination transport.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    Guid? MessageContext.MessageId => MessageId;
    DateTimeOffset? MessageContext.SentTime => SentTime;
    Headers MessageContext.Headers => _headers ?? EmptyHeaders.Instance;
    HostInfo MessageContext.Host => HostMetadataCache.Host;

    IReadOnlyDictionary<string, object> OutboxMessageContext.Properties => _properties;
    DurablePayloadAdmissionProof? IDurableOutboxMessageContext.AdmissionProof => _admissionProof;

    /// <summary>Materializes the persisted headers and transport properties for delivery.</summary>
    /// <param name="deserializer">The metadata deserializer used for both dictionaries.</param>
    public void Deserialize(IObjectDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(deserializer);

        (string? serializedHeaders, _admissionProof) = OutboxAdmissionMetadata.Decode(Headers);
        _headers = DeserializerHeaders(deserializer, serializedHeaders);
        _properties = DeserializerProperties(deserializer);
    }

    Headers DeserializerHeaders(IObjectDeserializer deserializer, string? serializedHeaders)
    {
        Dictionary<string, object?>? headers = deserializer.DeserializeDictionary<object?>(serializedHeaders);
        if (headers != null)
        {
            return new DictionarySendHeaders(headers
                .Where(static pair => pair.Value is not null)
                .Select(static pair => new KeyValuePair<string, object>(pair.Key, pair.Value!)));
        }

        return EmptyHeaders.Instance;
    }

    IReadOnlyDictionary<string, object> DeserializerProperties(IObjectDeserializer deserializer)
    {
        Dictionary<string, object>? properties = deserializer.DeserializeDictionary<object>(Properties);

        return properties?.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
            ?? FrozenDictionary<string, object>.Empty;
    }
}
