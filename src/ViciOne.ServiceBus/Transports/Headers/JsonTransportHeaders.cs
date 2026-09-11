using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Reads transport headers and converts JSON-backed values to their requested CLR types.</summary>
public sealed class JsonTransportHeaders :
    Headers
{
    readonly IHeaderProvider _provider;

    /// <summary>Initializes a header collection backed by the specified provider.</summary>
    /// <param name="provider">The provider that supplies raw transport headers.</param>
    public JsonTransportHeaders(IHeaderProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Returns every raw header supplied by the transport.</summary>
    /// <returns>The raw header sequence.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _provider.GetAll()
            ?? throw new InvalidOperationException("The transport header provider returned no header sequence.");
    }

    /// <summary>Attempts to retrieve a raw header value.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The raw value when the header exists.</param>
    /// <returns><see langword="true" /> when the header exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_provider.TryGetHeader(key, out value) && value is not null)
            return true;

        value = null;
        return false;
    }

    /// <summary>Gets a reference-type header after applying JSON-aware conversion.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="key">The header name.</param>
    /// <param name="defaultValue">The value returned when the header is absent or cannot be converted.</param>
    /// <returns>The converted header value or <paramref name="defaultValue" />.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(_provider, key, defaultValue);
    }

    /// <summary>Gets a value-type header after applying JSON-aware conversion.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="key">The header name.</param>
    /// <param name="defaultValue">The value returned when the header is absent or cannot be converted.</param>
    /// <returns>The converted header value or <paramref name="defaultValue" />.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(_provider, key, defaultValue);
    }

    /// <summary>Returns an enumerator over the transport headers.</summary>
    /// <returns>An enumerator that projects each raw value as a <see cref="HeaderValue" />.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return GetAll().Select(x => new HeaderValue(x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
