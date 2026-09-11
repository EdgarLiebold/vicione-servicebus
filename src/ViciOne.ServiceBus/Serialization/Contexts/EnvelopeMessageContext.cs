using System;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Projects a serialized envelope into the strongly typed message metadata contract.</summary>
public sealed class EnvelopeMessageContext :
    MessageContext
{
    readonly MessageEnvelope _envelope;
    readonly IObjectDeserializer _objectDeserializer;
    Guid? _conversationId;
    Guid? _correlationId;
    Uri? _destinationAddress;
    Uri? _faultAddress;
    Headers? _headers;
    Guid? _initiatorId;
    Guid? _messageId;
    Guid? _requestId;
    Uri? _responseAddress;
    Uri? _sourceAddress;

    /// <summary>Creates a metadata view over a deserialized envelope.</summary>
    /// <param name="envelope">The envelope to project.</param>
    /// <param name="objectDeserializer">The converter used for typed header values.</param>
    public EnvelopeMessageContext(MessageEnvelope envelope, IObjectDeserializer objectDeserializer)
    {
        _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        _objectDeserializer = objectDeserializer ?? throw new ArgumentNullException(nameof(objectDeserializer));
    }

    /// <summary>Gets the validated message identifier.</summary>
    public Guid? MessageId => _messageId ??= ConvertIdToGuid(_envelope.MessageId, nameof(MessageEnvelope.MessageId));
    /// <summary>Gets the validated request identifier.</summary>
    public Guid? RequestId => _requestId ??= ConvertIdToGuid(_envelope.RequestId, nameof(MessageEnvelope.RequestId));
    /// <summary>Gets the validated correlation identifier.</summary>
    public Guid? CorrelationId => _correlationId ??= ConvertIdToGuid(_envelope.CorrelationId, nameof(MessageEnvelope.CorrelationId));
    /// <summary>Gets the validated conversation identifier.</summary>
    public Guid? ConversationId => _conversationId ??= ConvertIdToGuid(_envelope.ConversationId, nameof(MessageEnvelope.ConversationId));
    /// <summary>Gets the validated initiating message identifier.</summary>
    public Guid? InitiatorId => _initiatorId ??= ConvertIdToGuid(_envelope.InitiatorId, nameof(MessageEnvelope.InitiatorId));
    /// <summary>Gets the envelope expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _envelope.ExpirationTime;
    /// <summary>Gets the validated absolute source endpoint address.</summary>
    public Uri? SourceAddress => _sourceAddress ??= ConvertToUri(_envelope.SourceAddress, nameof(MessageEnvelope.SourceAddress));
    /// <summary>Gets the validated absolute destination endpoint address.</summary>
    public Uri? DestinationAddress => _destinationAddress ??= ConvertToUri(_envelope.DestinationAddress, nameof(MessageEnvelope.DestinationAddress));
    /// <summary>Gets the validated absolute response endpoint address.</summary>
    public Uri? ResponseAddress => _responseAddress ??= ConvertToUri(_envelope.ResponseAddress, nameof(MessageEnvelope.ResponseAddress));
    /// <summary>Gets the validated absolute fault endpoint address.</summary>
    public Uri? FaultAddress => _faultAddress ??= ConvertToUri(_envelope.FaultAddress, nameof(MessageEnvelope.FaultAddress));
    /// <summary>Gets the envelope creation time.</summary>
    public DateTimeOffset? SentTime => _envelope.SentTime;
    /// <summary>Gets the non-null application headers.</summary>
    public Headers Headers => _headers ??= GetHeaders();
    /// <summary>Gets the sending host metadata or an empty host projection.</summary>
    public HostInfo Host => _envelope.Host ?? HostMetadataCache.Empty;

    Headers GetHeaders()
    {
        return _envelope.Headers == null
            ? EmptyHeaders.Instance
            : new ReadOnlyDictionaryHeaders(_objectDeserializer, _envelope.Headers);
    }

    static Guid? ConvertIdToGuid(string? id, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(id))
            return default;

        if (Guid.TryParse(id, out var messageId))
            return messageId;

        throw new FormatException($"Envelope property '{propertyName}' is not a valid GUID: '{id}'.");
    }

    static Uri? ConvertToUri(string? uri, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? address))
            throw new FormatException($"Envelope property '{propertyName}' is not a valid absolute URI: '{uri}'.");
        return address;
    }
}
