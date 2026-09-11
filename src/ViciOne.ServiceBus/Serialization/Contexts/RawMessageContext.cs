using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Projects transport headers into the metadata contract of a raw message.</summary>
internal sealed class RawMessageContext :
    MessageContext
{
    readonly RawSerializerOptions _options;
    readonly Headers _transportHeaders;

    Guid? _conversationId;
    Guid? _correlationId;
    Uri? _faultAddress;
    Headers? _headers;
    HostInfo? _host;
    Guid? _initiatorId;
    Guid? _messageId;
    Guid? _requestId;
    Uri? _responseAddress;
    DateTimeOffset? _sentTime;
    Uri? _sourceAddress;

    /// <summary>Creates a metadata projection for an incoming raw message.</summary>
    /// <param name="headers">The transport headers that carry raw-message metadata.</param>
    /// <param name="destinationAddress">The endpoint that received the message.</param>
    /// <param name="options">The raw-header projection options.</param>
    public RawMessageContext(Headers headers, Uri? destinationAddress, RawSerializerOptions options)
    {
        _transportHeaders = headers ?? throw new ArgumentNullException(nameof(headers));
        if ((options & ~RawSerializerOptions.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(options), options, "The raw serializer options contain unsupported flags.");

        DestinationAddress = destinationAddress;
        _options = options;
    }

    /// <summary>Gets the identifier supplied by the transport.</summary>
    public Guid? MessageId => _messageId ??= _transportHeaders.GetMessageId();
    /// <summary>Gets the request identifier supplied by the transport.</summary>
    public Guid? RequestId => _requestId ??= _transportHeaders.GetRequestId();
    /// <summary>Gets the correlation identifier supplied by the transport.</summary>
    public Guid? CorrelationId => _correlationId ??= _transportHeaders.GetCorrelationId();
    /// <summary>Gets the conversation identifier supplied by the transport.</summary>
    public Guid? ConversationId => _conversationId ??= _transportHeaders.GetConversationId();
    /// <summary>Gets the initiating message identifier supplied by the transport.</summary>
    public Guid? InitiatorId => _initiatorId ??= _transportHeaders.GetInitiatorId();
    /// <summary>Gets the raw message expiration time, which is unavailable without an envelope.</summary>
    public DateTimeOffset? ExpirationTime { get; } = default;
    /// <summary>Gets the source address supplied by the transport.</summary>
    public Uri? SourceAddress => _sourceAddress ??= _transportHeaders.GetSourceAddress();
    /// <summary>Gets the endpoint that received the raw message.</summary>
    public Uri? DestinationAddress { get; }
    /// <summary>Gets the response address supplied by the transport.</summary>
    public Uri? ResponseAddress => _responseAddress ??= _transportHeaders.GetResponseAddress();
    /// <summary>Gets the fault address supplied by the transport.</summary>
    public Uri? FaultAddress => _faultAddress ??= _transportHeaders.GetFaultAddress();

    /// <summary>Gets the timestamp encoded in a NewId-compatible message identifier, when available.</summary>
    public DateTimeOffset? SentTime => _sentTime ??= GetSentTime();

    /// <summary>Gets permitted application headers and diagnostic propagation metadata.</summary>
    public Headers Headers => _headers ??= new TransportHeaderFilter(_transportHeaders, _options);

    /// <summary>Gets the sending host metadata, or an empty host projection when it was not supplied.</summary>
    public HostInfo Host => _host ??= GetHostInfo();

    DateTimeOffset? GetSentTime()
    {
        try
        {
            DateTimeOffset? sentTime = MessageId?.ToNewId().Timestamp;

            return sentTime > DateTimeConstants.Epoch ? sentTime : default;
        }
        catch (Exception)
        {
            return default;
        }
    }

    HostInfo GetHostInfo()
    {
        return _transportHeaders.Get<HostInfo>(MessageHeaders.Host.Info) ?? HostMetadataCache.Empty;
    }


    sealed class TransportHeaderFilter :
        Headers
    {
        static readonly HashSet<string> TransportHeaderNames = new(StringComparer.OrdinalIgnoreCase)
        {
            MessageHeaders.MessageId,
            MessageHeaders.CorrelationId,
            MessageHeaders.ConversationId,
            MessageHeaders.RequestId,
            MessageHeaders.InitiatorId,
            MessageHeaders.SourceAddress,
            MessageHeaders.ResponseAddress,
            MessageHeaders.FaultAddress,
        };
        static readonly HashSet<string> DiagnosticPropagationHeaderNames = new(StringComparer.OrdinalIgnoreCase)
        {
            DiagnosticHeaders.ActivityId,
            DiagnosticHeaders.ActivityTraceState,
            DiagnosticHeaders.ActivityCorrelationContext,
            DiagnosticHeaders.ActivityPropagation,
        };

        readonly Headers _headers;
        readonly RawSerializerOptions _options;

        public TransportHeaderFilter(Headers headers, RawSerializerOptions options)
        {
            _headers = headers;
            _options = options;
        }

        public IEnumerator<HeaderValue> GetEnumerator()
        {
            return GetAll().Select(x => new HeaderValue(x.Key, x.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            if (!_options.HasFlag(RawSerializerOptions.CopyHeaders))
                yield break;

            foreach (KeyValuePair<string, object> header in _headers.GetAll())
            {
                if (ShouldInclude(header.Key))
                    yield return header;
            }
        }

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (ShouldInclude(key))
                return _headers.TryGetHeader(key, out value);

            value = null;
            return false;
        }

        public T? Get<T>(string key, T? defaultValue = default)
            where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return ShouldInclude(key) ? _headers.Get(key, defaultValue) : defaultValue;
        }

        public T? Get<T>(string key, T? defaultValue = null)
            where T : struct
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return ShouldInclude(key) ? _headers.Get(key, defaultValue) : defaultValue;
        }

        bool ShouldInclude(string key) =>
            _options.HasFlag(RawSerializerOptions.CopyHeaders)
            && (DiagnosticPropagationHeaderNames.Contains(key)
                || !key.StartsWith(MessageHeaders.Prefix, StringComparison.OrdinalIgnoreCase)
                && !TransportHeaderNames.Contains(key));
    }
}
