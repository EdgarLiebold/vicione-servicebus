using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries the envelope for json message.</summary>
public class JsonMessageEnvelope :
    MessageEnvelope
{
    Dictionary<string, object?>? _headers;

    /// <summary>Initializes a new instance.</summary>
    public JsonMessageEnvelope()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    public JsonMessageEnvelope(SendContext context, object message)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        Message = message;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageTypeNames">The message type names.</param>
    public JsonMessageEnvelope(MessageContext context, object message, string[] messageTypeNames)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context, messageTypeNames));
        Message = message;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="envelope">The envelope.</param>
    public JsonMessageEnvelope(MessageEnvelope envelope)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));
        Message = envelope.Message;
    }

    /// <summary>Gets or sets the message id.</summary>
    public string? MessageId { get; set; }
    /// <summary>Gets or sets the request id.</summary>
    public string? RequestId { get; set; }
    /// <summary>Gets or sets the correlation id.</summary>
    public string? CorrelationId { get; set; }
    /// <summary>Gets or sets the conversation id.</summary>
    public string? ConversationId { get; set; }
    /// <summary>Gets or sets the initiator id.</summary>
    public string? InitiatorId { get; set; }
    /// <summary>Gets or sets the source address.</summary>
    public string? SourceAddress { get; set; }
    /// <summary>Gets or sets the destination address.</summary>
    public string? DestinationAddress { get; set; }
    /// <summary>Gets or sets the response address.</summary>
    public string? ResponseAddress { get; set; }
    /// <summary>Gets or sets the fault address.</summary>
    public string? FaultAddress { get; set; }
    /// <summary>Gets or sets the message type.</summary>
    public string[]? MessageType { get; set; }
    /// <summary>Gets or sets the message.</summary>
    public object? Message { get; set; }
    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }
    /// <summary>Gets or sets the sent time.</summary>
    public DateTimeOffset? SentTime { get; set; }

    /// <summary>Gets or sets the headers.</summary>
    public Dictionary<string, object?> Headers
    {
        get => _headers ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        set => _headers = value;
    }

    /// <summary>Gets or sets the host.</summary>
    public HostInfo? Host { get; set; }

    /// <summary>Updates the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
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
