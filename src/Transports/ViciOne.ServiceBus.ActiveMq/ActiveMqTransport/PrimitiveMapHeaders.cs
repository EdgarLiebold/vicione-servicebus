using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Adapts an Apache NMS primitive map to mutable send headers.</summary>
public class PrimitiveMapHeaders :
    SendHeaders
{
    readonly IPrimitiveMap _properties;

    /// <summary>Creates a header adapter over a native message-property map.</summary>
    /// <param name="properties">The native property map to read and update.</param>
    public PrimitiveMapHeaders(IPrimitiveMap properties)
    {
        _properties = properties;
    }

    /// <summary>Sets or removes a string property.</summary>
    /// <param name="key">The property name.</param>
    /// <param name="value">The value to set, or <see langword="null" /> to remove the property.</param>
    public void Set(string key, string? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            _properties.Remove(key);
        else
            _properties[key] = value;
    }

    /// <summary>Sets or removes a property, optionally preserving an existing value.</summary>
    /// <param name="key">The property name.</param>
    /// <param name="value">The value to set, or <see langword="null" /> to remove an overwritten property.</param>
    /// <param name="overwrite">Whether an existing property may be changed or removed.</param>
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

    /// <summary>Tries to read a non-null native message property.</summary>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value, when available.</param>
    /// <returns><see langword="true" /> when a non-null value is present; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Enumerates all non-null native message properties.</summary>
    /// <returns>The available property name/value pairs.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        foreach (string key in _properties.Keys)
        {
            var value = _properties[key];

            if (value != null)
                yield return new KeyValuePair<string, object>(key, value);
        }
    }

    /// <summary>Rejects object-based reference-type retrieval, which native primitive headers do not support.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="key">The property name; object-based retrieval is unsupported for every key.</param>
    /// <param name="defaultValue">The unused fallback value.</param>
    /// <returns>This method does not return.</returns>
    public T Get<T>(string key, T? defaultValue)
        where T : class
    {
        throw new NotSupportedException("Primitive ActiveMQ headers do not support object-based retrieval.");
    }

    /// <summary>Rejects object-based value-type retrieval, which native primitive headers do not support.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="key">The property name; object-based retrieval is unsupported for every key.</param>
    /// <param name="defaultValue">The unused fallback value.</param>
    /// <returns>This method does not return.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        throw new NotSupportedException("Primitive ActiveMQ headers do not support object-based retrieval.");
    }

    /// <summary>Enumerates all non-null properties as header values.</summary>
    /// <returns>An enumerator over the native properties.</returns>
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
