using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for transport set header adapter.
/// </summary>
/// <typeparam name="TValueType">The t value type type.</typeparam>
public interface ITransportSetHeaderAdapter<TValueType>
{
    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="dictionary">The dictionary value.</param>
    /// <param name="headerValue">The header value value.</param>
    void Set(IDictionary<string, TValueType> dictionary, in HeaderValue headerValue);
    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="dictionary">The dictionary value.</param>
    /// <param name="headerValue">The header value value.</param>
    void Set<T>(IDictionary<string, TValueType> dictionary, in HeaderValue<T> headerValue);
}
