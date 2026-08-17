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
        if (context.MessageId.HasValue)
            MessageId = context.MessageId.Value.ToString();

        if (context.RequestId.HasValue)
            RequestId = context.RequestId.Value.ToString();

        if (context.CorrelationId.HasValue)
            CorrelationId = context.CorrelationId.Value.ToString();

        if (context.ConversationId.HasValue)
            ConversationId = context.ConversationId.Value.ToString();

        if (context.InitiatorId.HasValue)
            InitiatorId = context.InitiatorId.Value.ToString();

        if (context.SourceAddress != null)
            SourceAddress = context.SourceAddress.ToString();

        if (context.DestinationAddress != null)
            DestinationAddress = context.DestinationAddress.ToString();

        if (context.ResponseAddress != null)
            ResponseAddress = context.ResponseAddress.ToString();

        if (context.FaultAddress != null)
            FaultAddress = context.FaultAddress.ToString();

        MessageType = context.SupportedMessageTypes;

        IsMessageNativeMessagePackSerialized = true;
        Message = InternalMessagePackResolver.Serialize(message);

        if (context.TimeToLive.HasValue)
            ExpirationTime = DateTime.UtcNow + context.TimeToLive;

        SentTime = context.SentTime ?? DateTime.UtcNow;

        Headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> header in context.Headers.GetAll())
            Headers[header.Key] = header.Value;

        Host = HostMetadataCache.Host;
    }

    public MessagePackEnvelope(MessageEnvelope envelope)
    {
        MessageId = envelope.MessageId;
        RequestId = envelope.RequestId;
        CorrelationId = envelope.CorrelationId;
        ConversationId = envelope.ConversationId;
        InitiatorId = envelope.InitiatorId;
        SourceAddress = envelope.SourceAddress;
        DestinationAddress = envelope.DestinationAddress;
        ResponseAddress = envelope.ResponseAddress;
        FaultAddress = envelope.FaultAddress;

        MessageType = envelope.MessageType;

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

        ExpirationTime = envelope.ExpirationTime;

        SentTime = envelope.SentTime ?? DateTime.UtcNow;

        Headers = envelope.Headers != null
            ? new Dictionary<string, object?>(envelope.Headers, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        Host = envelope.Host ?? HostMetadataCache.Host;
    }

    public MessagePackEnvelope(MessageContext context, object message, string[] messageTypesNames)
    {
        if (context.MessageId.HasValue)
            MessageId = context.MessageId.Value.ToString();

        if (context.RequestId.HasValue)
            RequestId = context.RequestId.Value.ToString();

        if (context.CorrelationId.HasValue)
            CorrelationId = context.CorrelationId.Value.ToString();

        if (context.ConversationId.HasValue)
            ConversationId = context.ConversationId.Value.ToString();

        if (context.InitiatorId.HasValue)
            InitiatorId = context.InitiatorId.Value.ToString();

        if (context.SourceAddress != null)
            SourceAddress = context.SourceAddress.ToString();

        if (context.DestinationAddress != null)
            DestinationAddress = context.DestinationAddress.ToString();

        if (context.ResponseAddress != null)
            ResponseAddress = context.ResponseAddress.ToString();

        if (context.FaultAddress != null)
            FaultAddress = context.FaultAddress.ToString();

        MessageType = messageTypesNames;

        IsMessageNativeMessagePackSerialized = true;
        Message = InternalMessagePackResolver.Serialize(message);

        ExpirationTime = context.ExpirationTime;

        SentTime = context.SentTime ?? DateTime.UtcNow;

        Headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> header in context.Headers.GetAll())
            Headers[header.Key] = header.Value;

        Host = HostMetadataCache.Host;
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
        DestinationAddress = context.DestinationAddress?.ToString();

        if (context.SourceAddress != null)
            SourceAddress = context.SourceAddress.ToString();

        if (context.ResponseAddress != null)
            ResponseAddress = context.ResponseAddress.ToString();

        if (context.FaultAddress != null)
            FaultAddress = context.FaultAddress.ToString();

        if (context.MessageId.HasValue)
            MessageId = context.MessageId.ToString();

        if (context.RequestId.HasValue)
            RequestId = context.RequestId.ToString();

        if (context.ConversationId.HasValue)
            ConversationId = context.ConversationId.ToString();

        if (context.CorrelationId.HasValue)
            CorrelationId = context.CorrelationId.ToString();

        if (context.InitiatorId.HasValue)
            InitiatorId = context.InitiatorId.ToString();

        if (context.TimeToLive.HasValue)
            ExpirationTime = DateTime.UtcNow + (context.TimeToLive > TimeSpan.Zero ? context.TimeToLive : TimeSpan.FromSeconds(1));

        Headers ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> header in context.Headers.GetAll())
            Headers[header.Key] = header.Value;

        if (MessageType != null)
            context.SupportedMessageTypes = MessageType;
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
