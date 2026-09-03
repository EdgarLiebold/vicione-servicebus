namespace ViciOne.ServiceBus.Serialization;

using System;
using System.Collections.Generic;
using MessagePack;
using Metadata;


public class MessagePackEnvelope :
    MessageEnvelope
{
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
    public bool IsMessageNativeMessagePackSerialized { get; set; }
    public object? Message { get; set; }
    public DateTime? ExpirationTime { get; set; }
    public DateTime? SentTime { get; set; }
    public Dictionary<string, object?>? Headers { get; set; }
    public HostInfo? Host { get; set; }

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
