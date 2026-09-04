using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

[Serializable]
public class JsonMessageEnvelope :
    MessageEnvelope
{
    Dictionary<string, object?>? _headers;

    public JsonMessageEnvelope()
    {
    }

    public JsonMessageEnvelope(SendContext context, object message)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        Message = message;
    }

    public JsonMessageEnvelope(MessageContext context, object message, string[] messageTypeNames)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context, messageTypeNames));
        Message = message;
    }

    public JsonMessageEnvelope(MessageEnvelope envelope)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));
        Message = envelope.Message;
    }

    public string? MessageId { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ConversationId { get; set; }
    public string? InitiatorId { get; set; }
    public string? SourceAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public string? ResponseAddress { get; set; }
    public string? FaultAddress { get; set; }
    public string[]? MessageType { get; set; }
    public object? Message { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public DateTimeOffset? SentTime { get; set; }

    public Dictionary<string, object?> Headers
    {
        get => _headers ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        set => _headers = value;
    }

    public HostInfo? Host { get; set; }

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
