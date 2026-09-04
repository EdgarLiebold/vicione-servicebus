using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a primitive map headers implementation.
/// </summary>
public class PrimitiveMapHeaders :
    SendHeaders
{
    readonly IPrimitiveMap _properties;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public PrimitiveMapHeaders(IPrimitiveMap properties)
    {
        _properties = properties;
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
            _properties.Remove(key);
        else
            _properties[key] = value;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="overwrite">The overwrite value.</param>
    public void Set(string key, object? value, bool overwrite)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (overwrite)
        {
            if (value == null)
                _properties.Remove(key);
            else
                _properties[key] = value;
        }
        else if (value != null && !_properties.Contains(key))
            _properties[key] = value;
    }

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        var found = _properties.Contains(key);
        if (found)
        {
            value = _properties[key];
            return value != null;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        foreach (string key in _properties.Keys)
        {
            var value = _properties[key];

            if (value != null)
                yield return new KeyValuePair<string, object>(key, value);
        }
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T Get<T>(string key, T? defaultValue)
        where T : class
    {
        throw new NotImplementedByDesignException("PrimitiveMapHeaders does not support object-based header retrieval");
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
        throw new NotImplementedByDesignException("PrimitiveMapHeaders does not support object-based header retrieval");
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        foreach (string key in _properties.Keys)
        {
            var value = _properties[key];

            if (value != null)
                yield return new HeaderValue(key, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
