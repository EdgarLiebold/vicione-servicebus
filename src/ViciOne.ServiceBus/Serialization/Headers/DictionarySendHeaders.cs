using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides a case-insensitive collection of non-null headers for an outgoing message.</summary>
public sealed class DictionarySendHeaders :
    SendHeaders
{
    readonly IDictionary<string, object> _headers;

    /// <summary>Creates an empty header collection.</summary>
    public DictionarySendHeaders()
    {
        _headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Creates an independent, case-insensitive copy of the supplied headers.</summary>
    /// <param name="headers">The headers to copy. Null-valued entries are omitted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="headers" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A header name is empty or consists only of white-space characters.</exception>
    public DictionarySendHeaders(IEnumerable<KeyValuePair<string, object>> headers)
        : this()
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (KeyValuePair<string, object> header in headers)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(header.Key);
            if (header.Value != null)
                _headers.Add(header.Key, header.Value);
        }
    }

    DictionarySendHeaders(IDictionary<string, object> headers)
    {
        _headers = headers;
    }

    /// <summary>Wraps an existing dictionary so changes remain visible through both references.</summary>
    /// <param name="headers">The dictionary that will store the headers.</param>
    /// <returns>A header collection backed by <paramref name="headers" />.</returns>
    /// <remarks>
    /// The supplied dictionary controls key comparison. Entries with invalid names or null values are never exposed
    /// through the <see cref="SendHeaders" /> contract.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="headers" /> is <see langword="null" />.</exception>
    public static DictionarySendHeaders Wrap(IDictionary<string, object> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        return new DictionarySendHeaders(headers);
    }

    /// <summary>Sets or removes a string-valued header.</summary>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="value">The header value, or <see langword="null" /> to remove the header.</param>
    public void Set(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value == null)
            _headers.Remove(key);
        else
            _headers[key] = value;
    }

    /// <summary>Sets, preserves, or removes an object-valued header.</summary>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="value">The header value, or <see langword="null" /> to remove an existing header.</param>
    /// <param name="overwrite">Whether an existing value may be replaced or removed.</param>
    public void Set(string key, object? value, bool overwrite = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

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

    /// <summary>Tries to obtain a non-null header value.</summary>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="value">The value when the header exists and is non-null.</param>
    /// <returns><see langword="true" /> when a non-null value exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _headers.TryGetValue(key, out value) && value is not null;
    }

    /// <summary>Enumerates headers whose names and values satisfy the send-header contract.</summary>
    /// <returns>The valid, non-null headers.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _headers.Where(IsValidHeader);
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
        if (!_headers.TryGetValue(key, out object? value) || value is null)
            return defaultValue;

        object? preparedValue = HeaderValueConversion.PrepareHeaderValue<T>(value, out bool incompatible, out bool useBuiltInFallback);
        if (incompatible)
            return defaultValue;

        try
        {
            return ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject(preparedValue, defaultValue);
        }
        catch (JsonException) when (useBuiltInFallback)
        {
            return defaultValue;
        }
    }

    /// <summary>Gets a value-type header value or the supplied fallback.</summary>
    /// <typeparam name="T">The requested header value type.</typeparam>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="defaultValue">The fallback for an absent, null, or incompatible value.</param>
    /// <returns>The converted header value or the fallback.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_headers.TryGetValue(key, out object? value) || value is null)
            return defaultValue;

        object? preparedValue = HeaderValueConversion.PrepareHeaderValue<T>(value, out bool incompatible, out bool useBuiltInFallback);
        if (incompatible)
            return defaultValue;

        try
        {
            return ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject(preparedValue, defaultValue);
        }
        catch (JsonException) when (useBuiltInFallback)
        {
            return defaultValue;
        }
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

    static bool IsValidHeader(KeyValuePair<string, object> header) =>
        !string.IsNullOrWhiteSpace(header.Key) && header.Value is not null;
}
