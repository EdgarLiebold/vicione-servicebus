using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Exposes deserialized envelope headers as non-null transport metadata.</summary>
internal sealed class ReadOnlyDictionaryHeaders :
    Headers
{
    readonly IObjectDeserializer _deserializer;
    readonly IReadOnlyDictionary<string, object?> _headers;

    /// <summary>Wraps envelope headers and uses the envelope serializer for typed conversion.</summary>
    /// <param name="deserializer">The serializer-compatible object converter.</param>
    /// <param name="headers">The headers to expose.</param>
    public ReadOnlyDictionaryHeaders(IObjectDeserializer deserializer, IReadOnlyDictionary<string, object?> headers)
    {
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    /// <summary>Enumerates headers that have a valid name and a non-null value.</summary>
    /// <returns>The valid envelope headers.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        foreach ((string key, object? value) in _headers)
        {
            if (!string.IsNullOrWhiteSpace(key) && value is not null)
                yield return new KeyValuePair<string, object>(key, value);
        }
    }

    /// <summary>Tries to obtain a non-null envelope header.</summary>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="value">The value when the header exists and is non-null.</param>
    /// <returns><see langword="true" /> when a non-null value exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _headers.TryGetValue(key, out value) && value is not null;
    }

    /// <summary>Gets a reference-type header value or the supplied fallback.</summary>
    /// <typeparam name="T">The requested header value type.</typeparam>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="defaultValue">The fallback for an absent, null, or incompatible value.</param>
    /// <returns>The converted header value or the fallback.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _headers.TryGetValue(key, out object? value) && value is not null
            ? _deserializer.DeserializeObject(value, defaultValue)
            : defaultValue;
    }

    /// <summary>Gets a value-type header value or the supplied fallback.</summary>
    /// <typeparam name="T">The requested header value type.</typeparam>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="defaultValue">The fallback for an absent, null, or incompatible value.</param>
    /// <returns>The converted header value or the fallback.</returns>
    public T? Get<T>(string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _headers.TryGetValue(key, out object? value) && value is not null
            ? _deserializer.DeserializeObject(value, defaultValue)
            : defaultValue;
    }

    /// <summary>Enumerates the valid headers as strongly validated header values.</summary>
    /// <returns>An enumerator over the valid headers.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return GetAll().Select(static x => new HeaderValue(x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
