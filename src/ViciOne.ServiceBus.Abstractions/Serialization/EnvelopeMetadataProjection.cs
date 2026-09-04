using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.ProviderAbstractions;

namespace ViciOne.ServiceBus.Serialization;
/// <summary>
/// Captures the serializer-independent part of a message envelope. Wire serializers own only the
/// payload encoding and copy these values into their wire model.
/// </summary>
internal readonly struct EnvelopeMetadataProjection
{
    private EnvelopeMetadataProjection(
        string? messageId,
        string? requestId,
        string? correlationId,
        string? conversationId,
        string? initiatorId,
        string? sourceAddress,
        string? destinationAddress,
        string? responseAddress,
        string? faultAddress,
        string[]? messageType,
        DateTime? expirationTime,
        DateTime? sentTime,
        Dictionary<string, object?> headers,
        HostInfo host)
    {
        MessageId = messageId;
        RequestId = requestId;
        CorrelationId = correlationId;
        ConversationId = conversationId;
        InitiatorId = initiatorId;
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        ResponseAddress = responseAddress;
        FaultAddress = faultAddress;
        MessageType = messageType;
        ExpirationTime = expirationTime;
        SentTime = sentTime;
        Headers = headers;
        Host = host;
    }

    public string? MessageId { get; }
    public string? RequestId { get; }
    public string? CorrelationId { get; }
    public string? ConversationId { get; }
    public string? InitiatorId { get; }
    public string? SourceAddress { get; }
    public string? DestinationAddress { get; }
    public string? ResponseAddress { get; }
    public string? FaultAddress { get; }
    public string[]? MessageType { get; }
    public DateTime? ExpirationTime { get; }
    public DateTime? SentTime { get; }
    public Dictionary<string, object?> Headers { get; }
    public HostInfo Host { get; }

    public static EnvelopeMetadataProjection From(SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool durableAdmission = context.TryGetPayload(out DurableSendEnvelopeMetadata? _);
        bool needsUtcNow = context.TimeToLive.HasValue || !context.SentTime.HasValue && !durableAdmission;
        DateTime utcNow = needsUtcNow ? GetUtcNow(context) : default;

        return new EnvelopeMetadataProjection(
            context.MessageId?.ToString(),
            context.RequestId?.ToString(),
            context.CorrelationId?.ToString(),
            context.ConversationId?.ToString(),
            context.InitiatorId?.ToString(),
            context.SourceAddress?.ToString(),
            context.DestinationAddress?.ToString(),
            context.ResponseAddress?.ToString(),
            context.FaultAddress?.ToString(),
            context.SupportedMessageTypes,
            context.TimeToLive.HasValue ? utcNow + context.TimeToLive.Value : null,
            context.SentTime ?? (durableAdmission ? null : utcNow),
            CopyHeaders(context.Headers),
            HostMetadataCache.Host);
    }

    public static EnvelopeMetadataProjection From(
        MessageContext context,
        string[] messageTypes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageTypes);

        DateTime sentTime = context.SentTime ?? GetUtcNow(context as PipeContext);

        return new EnvelopeMetadataProjection(
            context.MessageId?.ToString(),
            context.RequestId?.ToString(),
            context.CorrelationId?.ToString(),
            context.ConversationId?.ToString(),
            context.InitiatorId?.ToString(),
            context.SourceAddress?.ToString(),
            context.DestinationAddress?.ToString(),
            context.ResponseAddress?.ToString(),
            context.FaultAddress?.ToString(),
            messageTypes,
            context.ExpirationTime,
            sentTime,
            CopyHeaders(context.Headers),
            HostMetadataCache.Host);
    }

    public static EnvelopeMetadataProjection From(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        DateTime sentTime = envelope.SentTime ?? TimeProvider.System.GetUtcNow().UtcDateTime;

        return new EnvelopeMetadataProjection(
            envelope.MessageId,
            envelope.RequestId,
            envelope.CorrelationId,
            envelope.ConversationId,
            envelope.InitiatorId,
            envelope.SourceAddress,
            envelope.DestinationAddress,
            envelope.ResponseAddress,
            envelope.FaultAddress,
            envelope.MessageType,
            envelope.ExpirationTime,
            sentTime,
            CopyHeaders(envelope.Headers),
            envelope.Host ?? HostMetadataCache.Host);
    }

    public static EnvelopeMetadataProjection Overlay(
        MessageEnvelope envelope,
        SendContext context)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        bool needsUtcNow = context.TimeToLive.HasValue
            || (!envelope.SentTime.HasValue && !context.SentTime.HasValue);
        DateTime utcNow = needsUtcNow ? GetUtcNow(context) : default;

        return new EnvelopeMetadataProjection(
            context.MessageId?.ToString() ?? envelope.MessageId,
            context.RequestId?.ToString() ?? envelope.RequestId,
            context.CorrelationId?.ToString() ?? envelope.CorrelationId,
            context.ConversationId?.ToString() ?? envelope.ConversationId,
            context.InitiatorId?.ToString() ?? envelope.InitiatorId,
            context.SourceAddress?.ToString() ?? envelope.SourceAddress,
            context.DestinationAddress?.ToString(),
            context.ResponseAddress?.ToString() ?? envelope.ResponseAddress,
            context.FaultAddress?.ToString() ?? envelope.FaultAddress,
            envelope.MessageType,
            context.TimeToLive.HasValue ? utcNow + context.TimeToLive.Value : envelope.ExpirationTime,
            envelope.SentTime ?? context.SentTime ?? utcNow,
            MergeHeaders(envelope.Headers, context.Headers),
            envelope.Host ?? HostMetadataCache.Host);
    }

    private static DateTime GetUtcNow(PipeContext? context)
    {
        TimeProvider timeProvider = context?.GetTimeProvider() ?? TimeProvider.System;

        return timeProvider.GetUtcNow().UtcDateTime;
    }

    private static Dictionary<string, object?> CopyHeaders(Headers? headers)
    {
        var copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (headers is null)
            return copy;

        foreach (KeyValuePair<string, object> header in headers.GetAll())
            copy[header.Key] = header.Value;

        return copy;
    }

    private static Dictionary<string, object?> CopyHeaders(
        IReadOnlyDictionary<string, object?>? headers)
    {
        return headers is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(headers, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> MergeHeaders(
        IReadOnlyDictionary<string, object?>? envelopeHeaders,
        Headers contextHeaders)
    {
        Dictionary<string, object?> merged = CopyHeaders(envelopeHeaders);

        foreach (KeyValuePair<string, object> header in contextHeaders.GetAll())
            merged[header.Key] = header.Value;

        return merged;
    }
}
