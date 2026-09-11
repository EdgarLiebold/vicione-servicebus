using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Providers.Persistence;

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
        IReadOnlyList<string>? messageTypes,
        DateTimeOffset? expirationTime,
        DateTimeOffset? sentTime,
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
        MessageTypes = messageTypes is null ? null : [.. messageTypes];
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
    public string[]? MessageTypes { get; }
    public DateTimeOffset? ExpirationTime { get; }
    public DateTimeOffset? SentTime { get; }
    public Dictionary<string, object?> Headers { get; }
    public HostInfo Host { get; }

    public static EnvelopeMetadataProjection From(SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool durableAdmission = context.TryGetPayload(out DurableSendEnvelopeMetadata? _);
        bool needsUtcNow = context.TimeToLive.HasValue || !context.SentTime.HasValue && !durableAdmission;
        DateTimeOffset utcNow = needsUtcNow ? GetUtcNow(context) : default;

        return new EnvelopeMetadataProjection(
            ToText(context.MessageId),
            ToText(context.RequestId),
            ToText(context.CorrelationId),
            ToText(context.ConversationId),
            ToText(context.InitiatorId),
            ToText(context.SourceAddress),
            ToText(context.DestinationAddress),
            ToText(context.ResponseAddress),
            ToText(context.FaultAddress),
            context.SupportedMessageTypes,
            context.TimeToLive.HasValue ? utcNow + context.TimeToLive.Value : null,
            context.SentTime ?? (durableAdmission ? null : utcNow),
            CopyHeaders(context.Headers),
            HostMetadataCache.Host);
    }

    public static EnvelopeMetadataProjection From(
        MessageContext context,
        IReadOnlyList<string> messageTypes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageTypes);

        DateTimeOffset sentTime = context.SentTime ?? GetUtcNow(context as PipeContext);

        return new EnvelopeMetadataProjection(
            ToText(context.MessageId),
            ToText(context.RequestId),
            ToText(context.CorrelationId),
            ToText(context.ConversationId),
            ToText(context.InitiatorId),
            ToText(context.SourceAddress),
            ToText(context.DestinationAddress),
            ToText(context.ResponseAddress),
            ToText(context.FaultAddress),
            messageTypes,
            context.ExpirationTime,
            sentTime,
            CopyHeaders(context.Headers),
            HostMetadataCache.Host);
    }

    public static EnvelopeMetadataProjection From(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

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
            envelope.MessageTypes,
            envelope.ExpirationTime,
            envelope.SentTime,
            CopyHeaders(envelope.Headers),
            envelope.Host ?? HostMetadataCache.Host);
    }

    public static EnvelopeMetadataProjection Overlay(
        MessageEnvelope envelope,
        SendContext context)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        bool durableAdmission = context.TryGetPayload(out DurableSendEnvelopeMetadata? _);
        bool needsUtcNow = context.TimeToLive.HasValue
            || (!durableAdmission && !envelope.SentTime.HasValue && !context.SentTime.HasValue);
        DateTimeOffset utcNow = needsUtcNow ? GetUtcNow(context) : default;

        return new EnvelopeMetadataProjection(
            PreferContext(context.MessageId, envelope.MessageId),
            PreferContext(context.RequestId, envelope.RequestId),
            PreferContext(context.CorrelationId, envelope.CorrelationId),
            PreferContext(context.ConversationId, envelope.ConversationId),
            PreferContext(context.InitiatorId, envelope.InitiatorId),
            PreferContext(context.SourceAddress, envelope.SourceAddress),
            ToText(context.DestinationAddress),
            PreferContext(context.ResponseAddress, envelope.ResponseAddress),
            PreferContext(context.FaultAddress, envelope.FaultAddress),
            envelope.MessageTypes,
            ResolveExpiration(context.TimeToLive, envelope.ExpirationTime, utcNow),
            ResolveSentTime(durableAdmission, envelope.SentTime, context.SentTime, utcNow),
            MergeHeaders(envelope.Headers, context.Headers),
            envelope.Host ?? HostMetadataCache.Host);
    }

    private static string? ToText<T>(T? value)
        where T : struct => value?.ToString();

    private static string? ToText(Uri? value) => value?.ToString();

    private static string? PreferContext<T>(T? contextValue, string? envelopeValue)
        where T : struct => ToText(contextValue) ?? envelopeValue;

    private static string? PreferContext(Uri? contextValue, string? envelopeValue) =>
        ToText(contextValue) ?? envelopeValue;

    private static DateTimeOffset? ResolveExpiration(
        TimeSpan? timeToLive,
        DateTimeOffset? envelopeExpirationTime,
        DateTimeOffset utcNow) => timeToLive.HasValue ? utcNow + timeToLive.Value : envelopeExpirationTime;

    private static DateTimeOffset? ResolveSentTime(
        bool durableAdmission,
        DateTimeOffset? envelopeSentTime,
        DateTimeOffset? contextSentTime,
        DateTimeOffset utcNow) => durableAdmission ? contextSentTime : envelopeSentTime ?? contextSentTime ?? utcNow;

    private static DateTimeOffset GetUtcNow(PipeContext? context)
    {
        TimeProvider timeProvider = context?.GetTimeProvider() ?? TimeProvider.System;

        return timeProvider.GetUtcNow();
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
