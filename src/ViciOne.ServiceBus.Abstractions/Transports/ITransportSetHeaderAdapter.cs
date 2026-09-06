using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by transport set header adapter.</summary>
/// <typeparam name="TValueType">The value type type.</typeparam>
public interface ITransportSetHeaderAdapter<TValueType>
{
    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValue">The header value to convert or store.</param>
    void Set(IDictionary<string, TValueType> dictionary, in HeaderValue headerValue);
    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValue">The header value to convert or store.</param>
    void Set<T>(IDictionary<string, TValueType> dictionary, in HeaderValue<T> headerValue);
}
