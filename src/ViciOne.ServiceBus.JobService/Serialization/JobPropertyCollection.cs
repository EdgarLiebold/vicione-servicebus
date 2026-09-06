using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Stores case-insensitive job metadata and converts values through the configured metadata serializer.</summary>
internal sealed class JobPropertyCollection :
    ISetPropertyCollection
{
    readonly Dictionary<string, object> _properties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the property values used to create job-service commands and events.</summary>
    internal Dictionary<string, object> Values => _properties;

    /// <summary>Gets the number of stored properties.</summary>
    public int Count => _properties.Count;

    /// <summary>Attempts to get a property without converting its value.</summary>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="value">Receives the stored value when present.</param>
    /// <returns><see langword="true" /> when the property exists; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties.TryGetValue(key, out value);
    }

    /// <summary>Gets a reference-type property after applying metadata conversion.</summary>
    /// <typeparam name="TValue">The expected property type.</typeparam>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or incompatible.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
    public TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(
            (IReadOnlyDictionary<string, object>)_properties,
            key,
            defaultValue);
    }

    /// <summary>Gets a value-type property after applying metadata conversion.</summary>
    /// <typeparam name="TValue">The expected property type.</typeparam>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or incompatible.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
    public TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(
            (IReadOnlyDictionary<string, object>)_properties,
            key,
            defaultValue);
    }

    /// <summary>Sets a property, optionally preserving an existing value.</summary>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="value">The value to store, or <see langword="null" /> to remove an existing property.</param>
    /// <param name="overwrite"><see langword="true" /> to replace an existing value; <see langword="false" /> to preserve it.</param>
    /// <returns>This property collection.</returns>
    public ISetPropertyCollection Set(string key, object? value, bool overwrite = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (overwrite)
        {
            if (value is null)
                _properties.Remove(key);
            else
                _properties[key] = value;
        }
        else if (value is not null)
            _properties.TryAdd(key, value);

        return this;
    }

    /// <summary>Merges properties into the collection; null values remove their corresponding keys.</summary>
    /// <param name="properties">The properties to merge.</param>
    /// <param name="overwrite"><see langword="true" /> to replace existing values; <see langword="false" /> to preserve them.</param>
    /// <returns>This property collection.</returns>
    public ISetPropertyCollection SetMany(IEnumerable<KeyValuePair<string, object?>> properties, bool overwrite = true)
    {
        ArgumentNullException.ThrowIfNull(properties);

        KeyValuePair<string, object?>[] entries = [.. properties];
        foreach (KeyValuePair<string, object?> entry in entries)
            ArgumentException.ThrowIfNullOrWhiteSpace(entry.Key, nameof(properties));

        foreach (KeyValuePair<string, object?> entry in entries)
            Set(entry.Key, entry.Value, overwrite);

        return this;
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        return _properties.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _properties.GetEnumerator();
    }

    int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _properties.Count;

    bool IReadOnlyDictionary<string, object>.ContainsKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties.ContainsKey(key);
    }

    bool IReadOnlyDictionary<string, object>.TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties.TryGetValue(key, out value);
    }

    object IReadOnlyDictionary<string, object>.this[string key]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return _properties[key];
        }
    }

    IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _properties.Keys;

    IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _properties.Values;
}
