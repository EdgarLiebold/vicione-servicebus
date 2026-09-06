using System;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides an envelope message context implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="envelope">The envelope value.</param>
    /// <param name="objectDeserializer">The object deserializer value.</param>
    public EnvelopeMessageContext(MessageEnvelope envelope, IObjectDeserializer objectDeserializer)
    {
        _envelope = envelope;
        _objectDeserializer = objectDeserializer;
    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid? MessageId => _messageId ??= ConvertIdToGuid(_envelope.MessageId);
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId => _requestId ??= ConvertIdToGuid(_envelope.RequestId);
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public Guid? CorrelationId => _correlationId ??= ConvertIdToGuid(_envelope.CorrelationId);
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public Guid? ConversationId => _conversationId ??= ConvertIdToGuid(_envelope.ConversationId);
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public Guid? InitiatorId => _initiatorId ??= ConvertIdToGuid(_envelope.InitiatorId);
    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime => _envelope.ExpirationTime;
    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public Uri? SourceAddress => _sourceAddress ??= ConvertToUri(_envelope.SourceAddress);
    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public Uri? DestinationAddress => _destinationAddress ??= ConvertToUri(_envelope.DestinationAddress);
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri? ResponseAddress => _responseAddress ??= ConvertToUri(_envelope.ResponseAddress);
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public Uri? FaultAddress => _faultAddress ??= ConvertToUri(_envelope.FaultAddress);
    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => _envelope.SentTime;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _headers ??= GetHeaders();
    /// <summary>
    /// Gets the host value.
    /// </summary>
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
