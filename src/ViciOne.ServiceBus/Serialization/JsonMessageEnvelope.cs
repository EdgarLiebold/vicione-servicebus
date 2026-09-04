using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a json message envelope implementation.
/// </summary>
[Serializable]
public class JsonMessageEnvelope :
    MessageEnvelope
{
    Dictionary<string, object?>? _headers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JsonMessageEnvelope()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    public JsonMessageEnvelope(SendContext context, object message)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageTypeNames">The message type names value.</param>
    public JsonMessageEnvelope(MessageContext context, object message, string[] messageTypeNames)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context, messageTypeNames));
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="envelope">The envelope value.</param>
    public JsonMessageEnvelope(MessageEnvelope envelope)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));
        Message = envelope.Message;
    }

    /// <summary>
    /// Gets or sets the message id value.
    /// </summary>
    public string? MessageId { get; set; }
    /// <summary>
    /// Gets or sets the request id value.
    /// </summary>
    public string? RequestId { get; set; }
    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    public string? CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the conversation id value.
    /// </summary>
    public string? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the initiator id value.
    /// </summary>
    public string? InitiatorId { get; set; }
    /// <summary>
    /// Gets or sets the source address value.
    /// </summary>
    public string? SourceAddress { get; set; }
    /// <summary>
    /// Gets or sets the destination address value.
    /// </summary>
    public string? DestinationAddress { get; set; }
    /// <summary>
    /// Gets or sets the response address value.
    /// </summary>
    public string? ResponseAddress { get; set; }
    /// <summary>
    /// Gets or sets the fault address value.
    /// </summary>
    public string? FaultAddress { get; set; }
    /// <summary>
    /// Gets or sets the message type value.
    /// </summary>
    public string[]? MessageType { get; set; }
    /// <summary>
    /// Gets or sets the message value.
    /// </summary>
    public object? Message { get; set; }
    /// <summary>
    /// Gets or sets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }
    /// <summary>
    /// Gets or sets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime { get; set; }

    /// <summary>
    /// Gets or sets the headers value.
    /// </summary>
    public Dictionary<string, object?> Headers
    {
        get => _headers ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        set => _headers = value;
    }

    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo? Host { get; set; }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Update(SendContext context)
    {
        ApplyMetadata(EnvelopeMetadataProjection.Overlay(this, context));

        if (MessageType != null)
            context.SupportedMessageTypes = MessageType;
    }

    void ApplyMetadata(EnvelopeMetadataProjection metadata)
    {
        MessageId = metadata.MessageId;
        RequestId = metadata.RequestId;
        CorrelationId = metadata.CorrelationId;
        ConversationId = metadata.ConversationId;
        InitiatorId = metadata.InitiatorId;
        SourceAddress = metadata.SourceAddress;
        DestinationAddress = metadata.DestinationAddress;
        ResponseAddress = metadata.ResponseAddress;
        FaultAddress = metadata.FaultAddress;
        MessageType = metadata.MessageType;
        ExpirationTime = metadata.ExpirationTime;
        SentTime = metadata.SentTime;
        Headers = metadata.Headers;
        Host = metadata.Host;
    }
}
