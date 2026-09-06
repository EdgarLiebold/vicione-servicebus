using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// The context headers are sourced from the IContextHeaderProvider, with the use of a Json deserializer
/// to convert data types to objects as required. If the original headers are Json objects, those headers
/// are deserialized as well.
/// </summary>
public class JsonTransportHeaders :
    Headers
{
    readonly IHeaderProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public JsonTransportHeaders(IHeaderProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Gets all.</summary>
    /// <returns>The all.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _provider.GetAll();
    }

    /// <summary>Attempts to get header.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        return _provider.TryGetHeader(key, out value);
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The requested value.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : class
    {
        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(_provider, key, defaultValue);
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The requested value.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(_provider, key, defaultValue);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return _provider.GetAll().Select(x => new HeaderValue(x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
