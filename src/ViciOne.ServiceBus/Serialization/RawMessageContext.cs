using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries state for raw message operations.</summary>
public class RawMessageContext :
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="headers">The headers.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="options">The options that control the operation.</param>
    public RawMessageContext(Headers headers, Uri? destinationAddress, RawSerializerOptions options)
    {
        _transportHeaders = headers;
        DestinationAddress = destinationAddress;
        _options = options;
    }

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => _messageId ??= _transportHeaders.GetMessageId();
    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => _requestId ??= _transportHeaders.GetRequestId();
    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => _correlationId ??= _transportHeaders.GetCorrelationId();
    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => _conversationId ??= _transportHeaders.GetConversationId();
    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => _initiatorId ??= _transportHeaders.GetInitiatorId();
    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; } = default;
    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _sourceAddress ??= _transportHeaders.GetSourceAddress();
    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress { get; }
    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _responseAddress ??= _transportHeaders.GetResponseAddress();
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _faultAddress ??= _transportHeaders.GetFaultAddress();

    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => _sentTime ??= GetSentTime();

    /// <summary>Gets the headers.</summary>
    public Headers Headers => _headers ??= new TransportHeaderFilter(_transportHeaders, _options);

    /// <summary>Gets the host.</summary>
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
        return _transportHeaders.Get<HostInfo>(MessageHeaders.Host.Info)!;
    }


    class TransportHeaderFilter :
        Headers
    {
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
            if (_options.HasFlag(RawSerializerOptions.CopyHeaders))
            {
                foreach (KeyValuePair<string, object> header in _headers.GetAll())
                {
                    if (header.Key.StartsWith(MessageHeaders.Prefix, StringComparison.Ordinal))
                        continue;

                    switch (header.Key)
                    {
                        case MessageHeaders.MessageId:
                        case MessageHeaders.CorrelationId:
                        case MessageHeaders.ConversationId:
                        case MessageHeaders.RequestId:
                        case MessageHeaders.InitiatorId:
                        case MessageHeaders.SourceAddress:
                        case MessageHeaders.ResponseAddress:
                        case MessageHeaders.FaultAddress:
                            break;

                        default:
                            yield return header;
                            break;
                    }
                }
            }
        }

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            return _headers.TryGetHeader(key, out value);
        }

        public T? Get<T>(string key, T? defaultValue = default)
            where T : class
        {
            return _headers.Get(key, defaultValue);
        }

        public T? Get<T>(string key, T? defaultValue = null)
            where T : struct
        {
            return _headers.Get(key, defaultValue);
        }
    }
}
