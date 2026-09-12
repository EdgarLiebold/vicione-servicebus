using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Represents the MessagePack wire envelope and its transport metadata.</summary>
internal sealed class MessagePackEnvelope :
    MessageEnvelope
{
    /// <summary>Gets or sets the message identifier encoded in the envelope.</summary>
    public string? MessageId { get; set; }
    /// <summary>Gets or sets the request identifier used to correlate a response.</summary>
    public string? RequestId { get; set; }
    /// <summary>Gets or sets the application correlation identifier.</summary>
    public string? CorrelationId { get; set; }
    /// <summary>Gets or sets the identifier shared by all messages in a conversation.</summary>
    public string? ConversationId { get; set; }
    /// <summary>Gets or sets the identifier of the message that initiated the conversation.</summary>
    public string? InitiatorId { get; set; }
    /// <summary>Gets or sets the endpoint address from which the message was sent.</summary>
    public string? SourceAddress { get; set; }
    /// <summary>Gets or sets the intended destination endpoint address.</summary>
    public string? DestinationAddress { get; set; }
    /// <summary>Gets or sets the endpoint address to which a response should be sent.</summary>
    public string? ResponseAddress { get; set; }
    /// <summary>Gets or sets the endpoint address to which a fault should be sent.</summary>
    public string? FaultAddress { get; set; }
    /// <summary>Gets or sets the URNs of the message contracts represented by the payload.</summary>
    public string[]? MessageTypes { get; set; }
    IReadOnlyList<string>? MessageEnvelope.MessageTypes =>
        MessageTypes is null ? null : Array.AsReadOnly(MessageTypes);
    /// <summary>
    /// Gets or sets whether <see cref="Message" /> contains a natively serialized MessagePack payload
    /// rather than a MessagePack-encoded object dictionary that requires metadata projection.
    /// </summary>
    public bool IsNativeMessagePackPayload { get; set; }
    /// <summary>Gets or sets the encoded MessagePack payload.</summary>
    public byte[]? Message { get; set; }
    object? MessageEnvelope.Message => Message;
    /// <summary>Gets or sets the instant after which the message is expired.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }
    /// <summary>Gets or sets the instant at which the message was sent.</summary>
    public DateTimeOffset? SentTime { get; set; }
    /// <summary>Gets or sets application and transport-independent message headers.</summary>
    public Dictionary<string, object?>? Headers { get; set; }
    IReadOnlyDictionary<string, object?>? MessageEnvelope.Headers =>
        Headers is null ? null : new ReadOnlyDictionary<string, object?>(Headers);
    /// <summary>Gets or sets information about the producing host.</summary>
    public HostInfo? Host { get; set; }

    /// <summary>Captures send metadata and serializes the supplied message into a new envelope.</summary>
    /// <param name="context">The send context that supplies envelope metadata.</param>
    /// <param name="message">The message to serialize.</param>
    public MessagePackEnvelope(SendContext context, object message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        IsNativeMessagePackPayload = true;
        Message = MessagePackSerializationRuntime.Serialize(message);
    }

    internal MessagePackEnvelope(SendContext context, byte[] serializedMessage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(serializedMessage);
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        IsNativeMessagePackPayload = true;
        Message = (byte[])serializedMessage.Clone();
    }

    /// <summary>Creates a MessagePack envelope from another envelope without sharing mutable payload bytes.</summary>
    /// <param name="envelope">The source envelope and metadata.</param>
    public MessagePackEnvelope(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));

        if (envelope is MessagePackEnvelope alreadyMessagePack)
        {
            IsNativeMessagePackPayload = alreadyMessagePack.IsNativeMessagePackPayload;
            Message = alreadyMessagePack.Message is null
                ? null
                : (byte[])alreadyMessagePack.Message.Clone();
        }
        else
        {
            IsNativeMessagePackPayload = true;
            Message = MessagePackSerializationRuntime.Serialize(envelope.Message);
        }
    }

    internal MessagePackEnvelope(
        MessageEnvelope envelope,
        byte[] serializedMessage,
        bool isNativeMessagePackPayload)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(serializedMessage);
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));
        IsNativeMessagePackPayload = isNativeMessagePackPayload;
        Message = (byte[])serializedMessage.Clone();
    }

    /// <summary>Captures message metadata and serializes a payload whose supported contract URNs are supplied explicitly.</summary>
    /// <param name="context">The message context that supplies envelope metadata.</param>
    /// <param name="message">The message to serialize.</param>
    /// <param name="messageTypes">The supported message contract URNs.</param>
    public MessagePackEnvelope(MessageContext context, object message, IReadOnlyList<string> messageTypes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageTypes);
        ApplyMetadata(EnvelopeMetadataProjection.From(context, messageTypes));
        IsNativeMessagePackPayload = true;
        Message = MessagePackSerializationRuntime.Serialize(message);
    }

    MessagePackEnvelope()
    {
    }

    internal void Update<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ApplyMetadata(EnvelopeMetadataProjection.Overlay(this, context));

        if (MessageTypes != null)
            context.SupportedMessageTypes = [.. MessageTypes];
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
