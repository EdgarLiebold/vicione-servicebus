using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a job property collection implementation.
/// </summary>
public class JobPropertyCollection :
    ISetPropertyCollection
{
    Dictionary<string, object>? _properties;

    /// <summary>
    /// Gets or sets the properties value.
    /// </summary>
    public Dictionary<string, object> Properties
    {
        get => _properties ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        set => _properties = value;
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _properties?.Count ?? 0;

    /// <summary>
    /// Performs the try get operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string key, [NotNullWhen(true)] out object? value)
    {
        if (_properties != null)
            return _properties.TryGetValue(key, out value);

        value = null;
        return false;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? Get<T>(string key, T? defaultValue = default)
        where T : class
    {
        return _properties == null
            ? defaultValue
            : ServiceBusMetadataJson.ObjectDeserializer.GetValue((IReadOnlyDictionary<string, object>)_properties, key, defaultValue);
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? Get<T>(string key, T? defaultValue = default)
        where T : struct
    {
        return _properties == null
            ? defaultValue
            : ServiceBusMetadataJson.ObjectDeserializer.GetValue((IReadOnlyDictionary<string, object>)_properties, key, defaultValue);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public ISetPropertyCollection Set(string key, string? value)
    {
        Properties.SetValue(key, value);

        return this;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="overwrite">The overwrite value.</param>
    /// <returns>The result of the operation.</returns>
    public ISetPropertyCollection Set(string key, object? value, bool overwrite = true)
    {
        Properties.SetValue(key, value, overwrite);

        return this;
    }

    /// <summary>
    /// Sets many.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    /// <param name="overwrite">The overwrite value.</param>
    /// <returns>The result of the operation.</returns>
    public ISetPropertyCollection SetMany(IEnumerable<KeyValuePair<string, object?>>? properties, bool overwrite = true)
    {
        Properties.SetValues(properties, overwrite);

        return this;
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        return _properties?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _properties?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
    }

    int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _properties?.Count ?? 0;

    bool IReadOnlyDictionary<string, object>.ContainsKey(string key)
    {
        return _properties?.ContainsKey(key) ?? false;
    }

    bool IReadOnlyDictionary<string, object>.TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        if (_properties != null)
            return _properties.TryGetValue(key, out value);

        value = null!;
        return false;
    }

    object IReadOnlyDictionary<string, object>.this[string key] => _properties?[key] ?? throw new KeyNotFoundException(key);

    IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _properties?.Keys ?? Enumerable.Empty<string>();

    IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _properties?.Values ?? Enumerable.Empty<object>();

    /// <summary>
    /// Converts a value to the target dictionary type.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Dictionary<string, object>(JobPropertyCollection properties)
    {
        return properties.Properties;
    }
}
