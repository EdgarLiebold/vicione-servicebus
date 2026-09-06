using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Carries state for receive endpoint dispatcher receive operations.</summary>
public sealed class ReceiveEndpointDispatcherReceiveContext :
    BaseReceiveContext
{
    readonly MessageBody _body;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveEndpointContext">The receive endpoint context.</param>
    /// <param name="body">The body.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="payloads">The payloads.</param>
    public ReceiveEndpointDispatcherReceiveContext(ReceiveEndpointContext receiveEndpointContext, byte[] body, IReadOnlyDictionary<string, object> headers,
        params object[] payloads)
        : base(IsRedelivered(headers), receiveEndpointContext, payloads)
    {
        _body = new BytesMessageBody(body);

        HeaderProvider = new ReadOnlyDictionaryHeaderProvider(headers);
    }

    /// <summary>Gets the header provider.</summary>
    protected override IHeaderProvider HeaderProvider { get; }

    /// <summary>Gets the body.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    static bool IsRedelivered(IReadOnlyDictionary<string, object> headers)
    {
        if (headers.TryGetValue("DeliveryCount", out var value) && value is int deliveryCount && deliveryCount > 1)
            return true;

        if (headers.TryGetValue("ApproximateReceiveCount", out value) && int.TryParse(value.ToString(), out deliveryCount) && deliveryCount > 1)
            return true;

        return false;
    }


    class ReadOnlyDictionaryHeaderProvider :
        IHeaderProvider
    {
        readonly IReadOnlyDictionary<string, object> _headers;

        public ReadOnlyDictionaryHeaderProvider(IReadOnlyDictionary<string, object>? headers = default)
        {
            _headers = headers ?? new Dictionary<string, object>();
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            return _headers;
        }

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            return _headers.TryGetValue(key, out value);
        }
    }
}
