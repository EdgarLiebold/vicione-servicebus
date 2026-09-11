using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Represents the JSON wire envelope for a message and its ServiceBus metadata.</summary>
public sealed class JsonMessageEnvelope :
    MessageEnvelope
{
    Dictionary<string, object?>? _headers;

    /// <summary>Creates an empty envelope for JSON deserialization.</summary>
    public JsonMessageEnvelope()
    {
    }

    /// <summary>Creates an envelope from an outgoing message context.</summary>
    /// <param name="context">The outgoing metadata source.</param>
    /// <param name="message">The message value to place in the envelope.</param>
    public JsonMessageEnvelope(SendContext context, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        Message = message;
    }

    /// <summary>Creates an envelope from deserialized metadata and an explicit contract set.</summary>
    /// <param name="context">The deserialized metadata source.</param>
    /// <param name="message">The message value to place in the envelope.</param>
    /// <param name="messageTypeNames">The message contract URNs to publish.</param>
    public JsonMessageEnvelope(MessageContext context, object message, IReadOnlyList<string> messageTypeNames)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageTypeNames);
        ApplyMetadata(EnvelopeMetadataProjection.From(context, messageTypeNames));
        Message = message;
    }

    /// <summary>Copies the metadata and message from another envelope representation.</summary>
    /// <param name="envelope">The envelope to copy.</param>
    public JsonMessageEnvelope(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));
        Message = envelope.Message;
    }

    /// <summary>Gets or sets the message identifier in canonical GUID text form.</summary>
    public string? MessageId { get; set; }
    /// <summary>Gets or sets the request identifier in canonical GUID text form.</summary>
    public string? RequestId { get; set; }
    /// <summary>Gets or sets the correlation identifier in canonical GUID text form.</summary>
    public string? CorrelationId { get; set; }
    /// <summary>Gets or sets the conversation identifier in canonical GUID text form.</summary>
    public string? ConversationId { get; set; }
    /// <summary>Gets or sets the initiating message identifier in canonical GUID text form.</summary>
    public string? InitiatorId { get; set; }
    /// <summary>Gets or sets the absolute source endpoint address.</summary>
    public string? SourceAddress { get; set; }
    /// <summary>Gets or sets the absolute destination endpoint address.</summary>
    public string? DestinationAddress { get; set; }
    /// <summary>Gets or sets the absolute response endpoint address.</summary>
    public string? ResponseAddress { get; set; }
    /// <summary>Gets or sets the absolute fault endpoint address.</summary>
    public string? FaultAddress { get; set; }
    /// <summary>Gets or sets the ordered message contract URNs.</summary>
    public string[]? MessageTypes { get; set; }
    IReadOnlyList<string>? MessageEnvelope.MessageTypes =>
        MessageTypes is null ? null : Array.AsReadOnly(MessageTypes);
    /// <summary>Gets or sets the application message value.</summary>
    public object? Message { get; set; }
    /// <summary>Gets or sets the absolute message expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }
    /// <summary>Gets or sets the time at which the envelope was created.</summary>
    public DateTimeOffset? SentTime { get; set; }

    /// <summary>Gets or sets the case-insensitive application headers.</summary>
    public Dictionary<string, object?> Headers
    {
        get => _headers ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _headers = new Dictionary<string, object?>(value, StringComparer.OrdinalIgnoreCase);
        }
    }
    IReadOnlyDictionary<string, object?> MessageEnvelope.Headers =>
        new ReadOnlyDictionary<string, object?>(Headers);

    /// <summary>Gets or sets metadata that identifies the sending host.</summary>
    public HostInfo? Host { get; set; }

    /// <summary>Overlays metadata supplied by a new outgoing context while retaining absent envelope values.</summary>
    /// <param name="context">The outgoing metadata to apply.</param>
    public void Update(SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ApplyMetadata(EnvelopeMetadataProjection.Overlay(this, context));

        if (MessageTypes != null)
            context.SupportedMessageTypes = MessageTypes;
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
        MessageTypes = metadata.MessageTypes;
        ExpirationTime = metadata.ExpirationTime;
        SentTime = metadata.SentTime;
        Headers = metadata.Headers;
        Host = metadata.Host;
    }
}
