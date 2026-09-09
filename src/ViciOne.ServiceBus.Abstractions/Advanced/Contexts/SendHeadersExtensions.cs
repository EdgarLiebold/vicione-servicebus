using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides header-copy operations for outgoing messages.</summary>
public static class SendHeadersExtensions
{
    /// <summary>Copies all source headers into an outgoing header collection.</summary>
    /// <param name="sendHeaders">The destination header collection.</param>
    /// <param name="headers">The source header collection.</param>
    public static void CopyFrom(this SendHeaders sendHeaders, Headers headers)
    {
        ArgumentNullException.ThrowIfNull(sendHeaders);
        ArgumentNullException.ThrowIfNull(headers);

        foreach (HeaderValue header in headers)
            sendHeaders.Set(header.Key, header.Value);
    }

    /// <summary>Copies all source headers into a transport-specific header dictionary.</summary>
    /// <typeparam name="THeaderValue">The transport-specific header value type.</typeparam>
    /// <param name="adapter">The adapter that converts and writes header values.</param>
    /// <param name="sendHeaders">The destination header dictionary.</param>
    /// <param name="headers">The source header collection.</param>
    public static void CopyFrom<THeaderValue>(this ITransportSetHeaderAdapter<THeaderValue> adapter,
        IDictionary<string, THeaderValue> sendHeaders, Headers headers)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(sendHeaders);
        ArgumentNullException.ThrowIfNull(headers);

        foreach (HeaderValue header in headers)
            adapter.Set(sendHeaders, header);
    }
}
