using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a receive endpoint dispatcher receive context implementation.
/// </summary>
public sealed class ReceiveEndpointDispatcherReceiveContext :
    BaseReceiveContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="body">The body value.</param>
    /// <param name="headers">The headers value.</param>
    /// <param name="payloads">The payloads value.</param>
    public ReceiveEndpointDispatcherReceiveContext(ReceiveEndpointContext receiveEndpointContext, byte[] body, IReadOnlyDictionary<string, object> headers,
        params object[] payloads)
        : base(IsRedelivered(headers), receiveEndpointContext, payloads)
    {
        Body = new BytesMessageBody(body);

        HeaderProvider = new ReadOnlyDictionaryHeaderProvider(headers);
    }

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider { get; }

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body { get; }

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
