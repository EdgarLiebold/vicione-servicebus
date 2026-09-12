using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Reconstructs message metadata and headers from a fired Quartz trigger's merged job data.</summary>
internal sealed class QuartzScheduledMessageContext :
    Headers
{
    readonly IObjectDeserializer _objectDeserializer;

    readonly Guid? _conversationId;
    readonly Guid? _correlationId;
    readonly Uri? _destinationAddress;
    readonly DateTimeOffset? _expirationTime;
    readonly Uri? _faultAddress;
    readonly Guid? _initiatorId;
    readonly Guid _messageId;
    readonly Guid? _requestId;
    readonly Uri? _responseAddress;
    readonly Uri? _sourceAddress;

    readonly Headers _headers;
    readonly IReadOnlyDictionary<string, object>? _transportProperties;

    /// <summary>Initializes message metadata from a fired Quartz job's merged data map.</summary>
    /// <param name="executionContext">The fired Quartz job context.</param>
    /// <param name="objectDeserializer">The deserializer for persisted headers and transport properties.</param>
    public QuartzScheduledMessageContext(IJobExecutionContext executionContext, IObjectDeserializer objectDeserializer)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        JobDataMap jobDataMap = executionContext.MergedJobDataMap;
        _objectDeserializer = objectDeserializer ?? throw new ArgumentNullException(nameof(objectDeserializer));

        Guid? messageId = ReadOptionalGuid(jobDataMap, QuartzJobDataKeys.MessageId);
        _messageId = messageId ?? CreateGeneratedMessageId(executionContext, jobDataMap);

        _requestId = ReadOptionalGuid(jobDataMap, QuartzJobDataKeys.RequestId);
        _correlationId = ReadOptionalGuid(jobDataMap, QuartzJobDataKeys.CorrelationId);
        _conversationId = ReadOptionalGuid(jobDataMap, QuartzJobDataKeys.ConversationId);
        _initiatorId = ReadOptionalGuid(jobDataMap, QuartzJobDataKeys.InitiatorId);
        _expirationTime = ReadOptionalTimestamp(jobDataMap, QuartzJobDataKeys.ExpirationTime);
        _sourceAddress = ReadOptionalAddress(jobDataMap, QuartzJobDataKeys.SourceAddress);
        _destinationAddress = ReadOptionalAddress(jobDataMap, QuartzJobDataKeys.DestinationAddress);
        _responseAddress = ReadOptionalAddress(jobDataMap, QuartzJobDataKeys.ResponseAddress);
        _faultAddress = ReadOptionalAddress(jobDataMap, QuartzJobDataKeys.FaultAddress);
        _headers = GetHeaders(executionContext, jobDataMap);
        _transportProperties = GetTransportProperties(jobDataMap);
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
                return true;
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

        return Headers.TryGetHeader(key, out value);
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

    /// <summary>Gets the persisted message identifier or the deterministic identifier for this trigger occurrence.</summary>
    public Guid MessageId => _messageId;

    /// <summary>Gets the request identifier captured when the context was created.</summary>
    public Guid? RequestId => _requestId;

    /// <summary>Gets the correlation identifier captured when the context was created.</summary>
    public Guid? CorrelationId => _correlationId;

    /// <summary>Gets the conversation identifier captured when the context was created.</summary>
    public Guid? ConversationId => _conversationId;

    /// <summary>Gets the initiator identifier captured when the context was created.</summary>
    public Guid? InitiatorId => _initiatorId;

    /// <summary>Gets the original absolute expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _expirationTime;

    /// <summary>Gets the absolute source address captured when the context was created.</summary>
    public Uri? SourceAddress => _sourceAddress;

    /// <summary>Gets the absolute destination address captured when the context was created.</summary>
    public Uri? DestinationAddress => _destinationAddress;

    /// <summary>Gets the absolute response address captured when the context was created.</summary>
    public Uri? ResponseAddress => _responseAddress;

    /// <summary>Gets the absolute fault address captured when the context was created.</summary>
    public Uri? FaultAddress => _faultAddress;

    /// <summary>Gets user headers enriched with Quartz fire-time and schedule metadata.</summary>
    public Headers Headers => _headers;

    /// <summary>Gets the transport-specific properties captured when the message was scheduled.</summary>
    public IReadOnlyDictionary<string, object>? TransportProperties => _transportProperties;

    Headers GetHeaders(IJobExecutionContext executionContext, JobDataMap jobDataMap)
    {
        var headers = new DictionarySendHeaders();

        if (jobDataMap.TryGetValue(QuartzJobDataKeys.Headers, out object? value))
        {
            IEnumerable<KeyValuePair<string, object>>? headerElements =
                _objectDeserializer.DeserializeObject<IEnumerable<KeyValuePair<string, object>>>(value);

            if (headerElements != null)
            {
                foreach (KeyValuePair<string, object> element in headerElements)
                    headers.Set(element.Key, element.Value);
            }
        }

        headers.Set(MessageHeaders.Quartz.Sent, executionContext.FireTimeUtc);

        if (executionContext.ScheduledFireTimeUtc.HasValue)
            headers.Set(MessageHeaders.Quartz.Scheduled, executionContext.ScheduledFireTimeUtc);

        if (executionContext.NextFireTimeUtc.HasValue)
            headers.Set(MessageHeaders.Quartz.NextScheduled, executionContext.NextFireTimeUtc);

        if (executionContext.PreviousFireTimeUtc.HasValue)
            headers.Set(MessageHeaders.Quartz.PreviousSent, executionContext.PreviousFireTimeUtc);

        if (jobDataMap.TryGetValue(QuartzJobDataKeys.SchedulingTokenId, out var tokenId))
            headers.Set(MessageHeaders.SchedulingTokenId, tokenId);

        if (jobDataMap.TryGetString(QuartzJobDataKeys.ScheduleId, out string? scheduleId)
            && !string.IsNullOrWhiteSpace(scheduleId))
        {
            headers.Set(MessageHeaders.Quartz.ScheduleId, scheduleId);
        }

        if (jobDataMap.TryGetString(QuartzJobDataKeys.ScheduleGroup, out string? scheduleGroup)
            && !string.IsNullOrWhiteSpace(scheduleGroup))
        {
            headers.Set(MessageHeaders.Quartz.ScheduleGroup, scheduleGroup);
        }

        return headers;
    }

    IReadOnlyDictionary<string, object>? GetTransportProperties(JobDataMap jobDataMap)
    {
        if (!jobDataMap.TryGetValue(QuartzJobDataKeys.TransportProperties, out object? value))
            return null;

        IReadOnlyDictionary<string, object>? properties =
            _objectDeserializer.DeserializeObject<IReadOnlyDictionary<string, object>>(value);
        return properties is null
            ? null
            : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(properties, StringComparer.Ordinal));
    }

    static Guid? ReadOptionalGuid(JobDataMap jobData, string key)
    {
        if (!jobData.TryGetValue(key, out object? value))
            return null;

        if (value is string text && Guid.TryParseExact(text, "D", out Guid identifier))
            return identifier;

        throw new FormatException($"Quartz job data value '{key}' must be a canonical D-format GUID.");
    }

    static DateTimeOffset? ReadOptionalTimestamp(JobDataMap jobData, string key)
    {
        if (!jobData.TryGetValue(key, out object? value))
            return null;

        if (value is string text
            && DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
        {
            return timestamp;
        }

        throw new FormatException($"Quartz job data value '{key}' must be a round-trip timestamp.");
    }

    static Uri? ReadOptionalAddress(JobDataMap jobData, string key)
    {
        if (!jobData.TryGetValue(key, out object? value))
            return null;

        if (value is string text && Uri.TryCreate(text, UriKind.Absolute, out Uri? address))
            return address;

        throw new FormatException($"Quartz job data value '{key}' must be an absolute URI.");
    }

    private static Guid CreateGeneratedMessageId(IJobExecutionContext executionContext, JobDataMap jobData)
    {
        Guid seed = ReadOptionalGuid(jobData, QuartzJobDataKeys.MessageIdSeed)
            ?? throw new InvalidOperationException(
                $"Quartz job data value '{QuartzJobDataKeys.MessageIdSeed}' is required when no message identifier is stored.");
        DateTimeOffset scheduledFireTime = executionContext.ScheduledFireTimeUtc
            ?? throw new InvalidOperationException(
                "A scheduled fire time is required to generate a recurring message identifier.");
        TriggerKey triggerKey = executionContext.Trigger.Key;
        string identity = string.Create(
            CultureInfo.InvariantCulture,
            $"{seed:D}|{triggerKey.Group.Length}:{triggerKey.Group}|{triggerKey.Name.Length}:{triggerKey.Name}|{scheduledFireTime.UtcTicks}");
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(identity));

        return new Guid(digest.AsSpan(0, 16));
    }
}
