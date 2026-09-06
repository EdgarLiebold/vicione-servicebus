using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Quartz;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Reconstructs message metadata and headers from a fired Quartz trigger's merged job data.</summary>
public class JobDataMessageContext :
    MessageContext,
    Headers
{
    readonly IJobExecutionContext _executionContext;
    readonly JobDataMap _jobDataMap;
    readonly IObjectDeserializer _objectDeserializer;

    Guid? _conversationId;
    Guid? _correlationId;
    Uri? _destinationAddress;
    DateTimeOffset? _expirationTime;
    Uri? _faultAddress;
    Headers? _headers;
    HostInfo? _hostInfo;
    Guid? _initiatorId;
    Guid? _messageId;
    Guid? _requestId;
    Uri? _responseAddress;
    DateTimeOffset? _sentTime;
    Uri? _sourceAddress;

    /// <summary>Initializes message metadata from a fired Quartz job's merged data map.</summary>
    /// <param name="executionContext">The fired Quartz job context.</param>
    /// <param name="objectDeserializer">The deserializer for persisted headers and transport properties.</param>
    public JobDataMessageContext(IJobExecutionContext executionContext, IObjectDeserializer objectDeserializer)
    {
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _jobDataMap = executionContext.MergedJobDataMap;
        _objectDeserializer = objectDeserializer ?? throw new ArgumentNullException(nameof(objectDeserializer));

        Guid? messageId = _jobDataMap.TryGetString(nameof(MessageId), out var text) ? ConvertIdToGuid(text) : default;

        if (messageId.HasValue)
            _messageId = messageId;
        else
        {
            var newId = NewId.Next();

            _messageId = newId.ToGuid();
            _sentTime = newId.Timestamp;
        }
    }

    /// <summary>Returns an enumerator over reconstructed message headers.</summary>
    /// <returns>A header-value enumerator.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return Headers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Enumerates all reconstructed message headers.</summary>
    /// <returns>The header name/value pairs.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return Headers.GetAll();
    }

    /// <summary>Attempts to resolve a standard message property or persisted user header.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the resolved value when present.</param>
    /// <returns><see langword="true"/> when the property or header exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        switch (key)
        {
            case MessageHeaders.MessageId:
                value = MessageId;
                return value != null;
            case MessageHeaders.CorrelationId:
                value = CorrelationId;
                return value != null;
            case MessageHeaders.ConversationId:
                value = ConversationId;
                return value != null;
            case MessageHeaders.RequestId:
                value = RequestId;
                return value != null;
            case MessageHeaders.InitiatorId:
                value = InitiatorId;
                return value != null;
            case MessageHeaders.SourceAddress:
                value = SourceAddress;
                return value != null;
            case MessageHeaders.ResponseAddress:
                value = ResponseAddress;
                return value != null;
            case MessageHeaders.FaultAddress:
                value = FaultAddress;
                return value != null;
        }

        return _jobDataMap.TryGetValue(key, out value);
    }

    /// <summary>Deserializes a reference-type header value.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the header is absent or cannot be converted.</param>
    /// <returns>The converted header value, or <paramref name="defaultValue"/> when no value can be materialized.</returns>
    public T? Get<T>(string key, T? defaultValue = default)
        where T : class
    {
        return TryGetHeader(key, out var value) ? _objectDeserializer.DeserializeObject(value, defaultValue) : defaultValue;
    }

    /// <summary>Deserializes a value-type header value.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the header is absent or cannot be converted.</param>
    /// <returns>The converted header value, or <paramref name="defaultValue"/> when no value can be materialized.</returns>
    public T? Get<T>(string key, T? defaultValue = default)
        where T : struct
    {
        return TryGetHeader(key, out var value) ? _objectDeserializer.DeserializeObject(value, defaultValue) : defaultValue;
    }

    /// <summary>Gets the persisted message identifier or a generated identifier when none was stored.</summary>
    public Guid? MessageId => _messageId ??= _jobDataMap.TryGetValue(nameof(MessageId), out string? value) ? ConvertIdToGuid(value) : NewId.NextGuid();
    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => _requestId ??= _jobDataMap.TryGetValue(nameof(RequestId), out string? value) ? ConvertIdToGuid(value) : default;
    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => _correlationId ??= _jobDataMap.TryGetValue(nameof(CorrelationId), out string? value) ? ConvertIdToGuid(value) : default;
    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => _conversationId ??= _jobDataMap.TryGetValue(nameof(ConversationId), out string? value) ? ConvertIdToGuid(value) : default;
    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => _initiatorId ??= _jobDataMap.TryGetValue(nameof(InitiatorId), out string? value) ? ConvertIdToGuid(value) : default;

    /// <summary>Gets the original absolute expiration time.</summary>
    public DateTimeOffset? ExpirationTime =>
        _expirationTime ??= _jobDataMap.TryGetValue(nameof(ExpirationTime), out string? value) ? ConvertDateTime(value) : default;

    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _sourceAddress ??= _jobDataMap.TryGetValue(nameof(SourceAddress), out string? value) ? ConvertToUri(value) : default;

    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress =>
        _destinationAddress ??= _jobDataMap.TryGetValue(nameof(DestinationAddress), out string? value) ? ConvertToUri(value) : default;

    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _responseAddress ??= _jobDataMap.TryGetValue(nameof(ResponseAddress), out string? value) ? ConvertToUri(value) : default;
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _faultAddress ??= _jobDataMap.TryGetValue(nameof(FaultAddress), out string? value) ? ConvertToUri(value) : default;
    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime =>
        _sentTime ??= _jobDataMap.TryGetValue(nameof(SentTime), out object? value) ? ConvertDateTime(value) : default;
    /// <summary>Gets user headers enriched with Quartz fire-time and schedule metadata.</summary>
    public Headers Headers => _headers ??= GetHeaders();
    /// <summary>Gets the persisted sender host information, or empty host metadata when absent.</summary>
    public HostInfo Host => _hostInfo ??= _jobDataMap.TryGetValue(nameof(Host), out HostInfo? value) ? value! : HostMetadataCache.Empty;

    /// <summary>Gets the transport-specific properties captured when the message was scheduled.</summary>
    public IReadOnlyDictionary<string, object>? TransportProperties =>
        _jobDataMap.TryGetValue("TransportProperties", out object? value)
            ? _objectDeserializer.DeserializeObject<IReadOnlyDictionary<string, object>>(value)
            : default;

    Headers GetHeaders()
    {
        var headers = new DictionarySendHeaders();

        if (_jobDataMap.TryGetValue("HeadersAsJson", out object? value))
        {
            IEnumerable<KeyValuePair<string, object>>? headerElements =
                _objectDeserializer.DeserializeObject<IEnumerable<KeyValuePair<string, object>>>(value);

            if (headerElements != null)
            {
                foreach (KeyValuePair<string, object> element in headerElements)
                    headers.Set(element.Key, element.Value);
            }
        }

        headers.Set(MessageHeaders.Quartz.Sent, _executionContext.FireTimeUtc);

        if (_executionContext.ScheduledFireTimeUtc.HasValue)
            headers.Set(MessageHeaders.Quartz.Scheduled, _executionContext.ScheduledFireTimeUtc);

        if (_executionContext.NextFireTimeUtc.HasValue)
            headers.Set(MessageHeaders.Quartz.NextScheduled, _executionContext.NextFireTimeUtc);

        if (_executionContext.PreviousFireTimeUtc.HasValue)
            headers.Set(MessageHeaders.Quartz.PreviousSent, _executionContext.PreviousFireTimeUtc);

        if (_jobDataMap.TryGetValue("TokenId", out var tokenId))
            headers.Set(MessageHeaders.SchedulingTokenId, tokenId);

        if (!string.IsNullOrWhiteSpace(_executionContext.Trigger.Key.Name))
            headers.Set(MessageHeaders.Quartz.ScheduleId, QuartzTriggerKey.GetScheduleId(_executionContext.Trigger.Key));

        if (!string.IsNullOrWhiteSpace(_executionContext.Trigger.Key.Group))
            headers.Set(MessageHeaders.Quartz.ScheduleGroup, _executionContext.Trigger.Key.Group);

        return headers;
    }

    static DateTimeOffset? ConvertDateTime(object? value)
    {
        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset;
        if (value is DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Local
                ? new DateTimeOffset(dateTime).ToUniversalTime()
                : new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        string? text = value as string;
        if (string.IsNullOrWhiteSpace(text))
            return default;

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp)
            ? timestamp
            : default(DateTimeOffset?);
    }

    static Guid? ConvertIdToGuid(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return default;

        if (Guid.TryParse(id, out var messageId))
            return messageId;

        throw new FormatException("The Id was not a Guid: " + id);
    }

    static Uri? ConvertToUri(string? uri)
    {
        return string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri);
    }
}
