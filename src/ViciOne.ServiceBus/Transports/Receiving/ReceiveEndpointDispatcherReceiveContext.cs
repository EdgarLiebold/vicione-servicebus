using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts an in-process dispatch request to the receive-pipeline context contract.</summary>
internal sealed class ReceiveEndpointDispatcherReceiveContext :
    BaseReceiveContext
{
    readonly MessageBody _body;

    /// <summary>Initializes a receive context from caller-supplied body, headers, and payloads.</summary>
    /// <param name="receiveEndpointContext">The endpoint context that owns the receive pipeline.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="headers">The transport header values.</param>
    /// <param name="payloads">The additional context payloads.</param>
    public ReceiveEndpointDispatcherReceiveContext(ReceiveEndpointContext receiveEndpointContext, byte[] body, IReadOnlyDictionary<string, object> headers,
        params object[] payloads)
        : base(IsRedelivered(headers), receiveEndpointContext, payloads)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(payloads);

        _body = new BytesMessageBody(body);

        HeaderProvider = new ReadOnlyDictionaryHeaderProvider(headers);
    }

    /// <summary>Gets the read-only transport header source.</summary>
    protected override IHeaderProvider HeaderProvider { get; }

    /// <summary>Gets the serialized body after applying endpoint message limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    static bool IsRedelivered(IReadOnlyDictionary<string, object> headers)
    {
        if (headers.TryGetValue("DeliveryCount", out var value) && value is int deliveryCount && deliveryCount > 1)
            return true;

        if (headers.TryGetValue("ApproximateReceiveCount", out value)
            && value is not null
            && int.TryParse(value.ToString(), out deliveryCount)
            && deliveryCount > 1)
            return true;

        return false;
    }


    sealed class ReadOnlyDictionaryHeaderProvider :
        IHeaderProvider
    {
        readonly IReadOnlyDictionary<string, object> _headers;

        public ReadOnlyDictionaryHeaderProvider(IReadOnlyDictionary<string, object> headers)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            return _headers;
        }

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return _headers.TryGetValue(key, out value);
        }
    }
}
