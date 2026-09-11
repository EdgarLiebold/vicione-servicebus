using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides the canonical empty header set for operations that received no transport metadata.</summary>
public sealed class EmptyHeaders :
    Headers
{
    /// <summary>Gets the shared empty header collection.</summary>
    public static EmptyHeaders Instance { get; } = new();

    EmptyHeaders()
    {
    }

    /// <summary>Returns an empty header sequence.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return Enumerable.Empty<KeyValuePair<string, object>>();
    }

    /// <summary>Reports that no value exists for a valid header name.</summary>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="value">Always <see langword="null" />.</param>
    /// <returns>Always <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        value = default;
        return false;
    }

    /// <summary>Returns the supplied reference-type fallback for a valid header name.</summary>
    /// <typeparam name="TValue">The requested header value type.</typeparam>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <returns><paramref name="defaultValue" />.</returns>
    public TValue? Get<TValue>(string key, TValue? defaultValue)
        where TValue : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return defaultValue;
    }

    /// <summary>Returns the supplied value-type fallback for a valid header name.</summary>
    /// <typeparam name="TValue">The requested header value type.</typeparam>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <returns><paramref name="defaultValue" />.</returns>
    public TValue? Get<TValue>(string key, TValue? defaultValue = null)
        where TValue : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return defaultValue;
    }

    /// <summary>Returns an enumerator that contains no header values.</summary>
    /// <returns>An empty enumerator.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
