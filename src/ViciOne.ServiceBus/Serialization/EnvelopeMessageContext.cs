using System;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries state for envelope message operations.</summary>
public class EnvelopeMessageContext :
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="envelope">The envelope.</param>
    /// <param name="objectDeserializer">The object deserializer.</param>
    public EnvelopeMessageContext(MessageEnvelope envelope, IObjectDeserializer objectDeserializer)
    {
        _envelope = envelope;
        _objectDeserializer = objectDeserializer;
    }

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => _messageId ??= ConvertIdToGuid(_envelope.MessageId);
    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => _requestId ??= ConvertIdToGuid(_envelope.RequestId);
    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => _correlationId ??= ConvertIdToGuid(_envelope.CorrelationId);
    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => _conversationId ??= ConvertIdToGuid(_envelope.ConversationId);
    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => _initiatorId ??= ConvertIdToGuid(_envelope.InitiatorId);
    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _envelope.ExpirationTime;
    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _sourceAddress ??= ConvertToUri(_envelope.SourceAddress);
    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => _destinationAddress ??= ConvertToUri(_envelope.DestinationAddress);
    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _responseAddress ??= ConvertToUri(_envelope.ResponseAddress);
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _faultAddress ??= ConvertToUri(_envelope.FaultAddress);
    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => _envelope.SentTime;
    /// <summary>Gets the headers.</summary>
    public Headers Headers => _headers ??= GetHeaders();
    /// <summary>Gets the host.</summary>
    public HostInfo Host => _envelope.Host ?? HostMetadataCache.Empty;

    Headers GetHeaders()
    {
        return _envelope.Headers == null
            ? EmptyHeaders.Instance
            : new ReadOnlyDictionaryHeaders(_objectDeserializer, _envelope.Headers);
    }

    static Guid? ConvertIdToGuid(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return default;

        if (Guid.TryParse(id, out var messageId))
            return messageId;

        throw new FormatException("The Id was not a Guid: " + id);
    }

    static Uri? ConvertToUri(string? uri)
    {
        return string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri);
    }
}
