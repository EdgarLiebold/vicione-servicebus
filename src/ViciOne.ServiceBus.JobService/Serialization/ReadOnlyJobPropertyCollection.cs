using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides an isolated, case-insensitive view of job metadata to a running consumer.</summary>
internal sealed class ReadOnlyJobPropertyCollection :
    IPropertyCollection
{
    readonly Dictionary<string, object> _properties;

    /// <summary>Copies metadata into a collection that exposes no mutation capability.</summary>
    /// <param name="properties">The metadata to copy, or <see langword="null" /> for an empty collection.</param>
    public ReadOnlyJobPropertyCollection(IEnumerable<KeyValuePair<string, object>>? properties)
    {
        _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (properties is null)
            return;

        foreach (KeyValuePair<string, object> property in properties)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(property.Key, nameof(properties));
            _properties[property.Key] = property.Value;
        }
    }

    /// <inheritdoc />
    public int Count => _properties.Count;

    /// <inheritdoc />
    public bool TryGet(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(
            (IReadOnlyDictionary<string, object>)_properties,
            key,
            defaultValue);
    }

    /// <inheritdoc />
    public TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return ServiceBusMetadataJson.ObjectDeserializer.GetValue(
            (IReadOnlyDictionary<string, object>)_properties,
            key,
            defaultValue);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _properties.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public object this[string key]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return _properties[key];
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Keys => _properties.Keys;

    /// <inheritdoc />
    public IEnumerable<object> Values => _properties.Values;
}
