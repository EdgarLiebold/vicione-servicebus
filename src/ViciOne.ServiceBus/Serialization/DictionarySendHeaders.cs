using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a dictionary send headers implementation.
/// </summary>
public class DictionarySendHeaders :
    SendHeaders
{
    readonly IDictionary<string, object> _headers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public DictionarySendHeaders()
    {
        _headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="headers">The headers value.</param>
    public DictionarySendHeaders(IDictionary<string, object?>? headers)
        : this()
    {
        if (headers == null)
            return;

        foreach (KeyValuePair<string, object?> header in headers)
        {
            if (header.Value != null)
                _headers.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="headers">The headers value.</param>
    /// <param name="useExistingDictionary">The use existing dictionary value.</param>
    public DictionarySendHeaders(IDictionary<string, object> headers, bool useExistingDictionary)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        if (useExistingDictionary)
            _headers = headers;
        else
            _headers = new Dictionary<string, object>(headers, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, string? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            _headers.Remove(key);
        else
            _headers[key] = value;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="overwrite">The overwrite value.</param>
    public void Set(string key, object? value, bool overwrite = true)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (overwrite)
        {
            if (value == null)
                _headers.Remove(key);
            else
                _headers[key] = value;
        }
        else if (!_headers.ContainsKey(key) && value != null)
            _headers.Add(key, value);
    }

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        return _headers.TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _headers;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : class
    {
        return ServiceBusMetadataJson.ObjectDeserializer.GetValue((IReadOnlyDictionary<string, object>)_headers, key, defaultValue);
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        return ServiceBusMetadataJson.ObjectDeserializer.GetValue((IReadOnlyDictionary<string, object>)_headers, key, defaultValue);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return _headers.Select(x => new HeaderValue(x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
