using System;
using System.Collections.Generic;
using MessagePack;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a message pack envelope implementation.
/// </summary>
public class MessagePackEnvelope :
    MessageEnvelope
{
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
    /// Gets or sets the is message native message pack serialized value.
    /// </summary>
    public bool IsMessageNativeMessagePackSerialized { get; set; }
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
    public Dictionary<string, object?>? Headers { get; set; }
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo? Host { get; set; }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    public MessagePackEnvelope(SendContext context, object message)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        IsMessageNativeMessagePackSerialized = true;
        Message = InternalMessagePackResolver.Serialize(message);
    }

    internal MessagePackEnvelope(SendContext context, byte[] serializedMessage)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context));
        IsMessageNativeMessagePackSerialized = true;
        Message = serializedMessage ?? throw new ArgumentNullException(nameof(serializedMessage));
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="envelope">The envelope value.</param>
    public MessagePackEnvelope(MessageEnvelope envelope)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));

        if (envelope is MessagePackEnvelope alreadyMessagePack)
        {
            // The payload of a MessagePack envelope is already MessagePack, whether it came off the wire
            // or from an overlay. Serializing it again wrapped those bytes in a second encoding, and the
            // receiver then found a byte array where it expected the message. Delayed redelivery and
            // scheduling both clone an envelope, which is why a redelivered message was never consumed.
            //
            // What has to be carried over is the bytes, not the array. Message is public and settable and
            // holds a mutable array, so two envelopes sharing one would let either of them write into
            // what the other sends; the payload is copied at the boundary instead. That is a memory copy
            // against an encoding, which is the trade the defect above was about.
            IsMessageNativeMessagePackSerialized = alreadyMessagePack.IsMessageNativeMessagePackSerialized;
            Message = CopyPayload(alreadyMessagePack.Message);
        }
        else
        {
            IsMessageNativeMessagePackSerialized = true;
            Message = InternalMessagePackResolver.Serialize(envelope.Message);
        }

    }

    internal MessagePackEnvelope(MessageEnvelope envelope, byte[] serializedMessage)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(envelope));
        IsMessageNativeMessagePackSerialized = true;
        Message = serializedMessage ?? throw new ArgumentNullException(nameof(serializedMessage));
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageTypesNames">The message types names value.</param>
    public MessagePackEnvelope(MessageContext context, object message, string[] messageTypesNames)
    {
        ApplyMetadata(EnvelopeMetadataProjection.From(context, messageTypesNames));
        IsMessageNativeMessagePackSerialized = true;
        Message = InternalMessagePackResolver.Serialize(message);
    }

    /// <summary>
    /// Used for deserialization.
    /// </summary>
    MessagePackEnvelope()
    {
    }

    internal void Update<T>(SendContext<T> context)
        where T : class
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

    /// <summary>
    /// The payload of an envelope that is already MessagePack travels as bytes. It is copied rather than
    /// aliased because <see cref="Message" /> is public and mutable; a payload that is not a byte array
    /// is carried as it is, because there is nothing defined to copy.
    /// </summary>
    static object? CopyPayload(object? message)
    {
        return message is byte[] bytes ? bytes.Clone() : message;
    }
}
