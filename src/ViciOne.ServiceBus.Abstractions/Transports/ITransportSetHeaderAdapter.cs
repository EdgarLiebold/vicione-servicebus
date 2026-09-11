using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Writes service-bus header values into a transport-specific header dictionary.</summary>
/// <typeparam name="THeaderValue">The value representation accepted by the transport dictionary.</typeparam>
public interface ITransportSetHeaderAdapter<THeaderValue>
{
    /// <summary>Converts and writes a dynamically typed header value.</summary>
    /// <param name="dictionary">The destination transport dictionary.</param>
    /// <param name="headerValue">The named header value to write.</param>
    void Set(IDictionary<string, THeaderValue> dictionary, in HeaderValue headerValue);

    /// <summary>Converts and writes a statically typed header value.</summary>
    /// <typeparam name="TValue">The service-bus header value type.</typeparam>
    /// <param name="dictionary">The destination transport dictionary.</param>
    /// <param name="headerValue">The named header value to write.</param>
    void Set<TValue>(IDictionary<string, THeaderValue> dictionary, in HeaderValue<TValue> headerValue);
}
