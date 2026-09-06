using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// When a message envelope is deserialized, encapsulate the headers such that objects can be deserialized from the
/// body using the message deserializer.
/// </summary>
public class ReadOnlyDictionaryHeaders :
    Headers
{
    readonly IObjectDeserializer _deserializer;
    readonly IReadOnlyDictionary<string, object?> _headers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="deserializer">The deserializer.</param>
    /// <param name="headers">The headers.</param>
    public ReadOnlyDictionaryHeaders(IObjectDeserializer deserializer, IReadOnlyDictionary<string, object?> headers)
    {
        _deserializer = deserializer;

        _headers = headers ?? new Dictionary<string, object?>();
    }

    /// <summary>Gets all.</summary>
    /// <returns>The all.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return (IEnumerable<KeyValuePair<string, object>>)(object)_headers;
    }

    /// <summary>Attempts to get header.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, out object value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        bool found = _headers.TryGetValue(key, out object? headerValue);
        value = headerValue!;
        return found;
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The requested value.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : class
    {
        return _deserializer.GetValue(AsNonNullableDictionary(), key, defaultValue);
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The requested value.</returns>
    public T? Get<T>(string key, T? defaultValue = null)
        where T : struct
    {
        return _deserializer.GetValue(AsNonNullableDictionary(), key, defaultValue);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return _headers.Select(x => new HeaderValue(x.Key, x.Value!)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IReadOnlyDictionary<string, object> AsNonNullableDictionary() =>
        (IReadOnlyDictionary<string, object>)(object)_headers;
}
