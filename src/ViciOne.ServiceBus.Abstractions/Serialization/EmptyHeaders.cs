using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides an empty headers implementation.
/// </summary>
public class EmptyHeaders :
    Headers
{
    /// <summary>
    /// Defines the instance value.
    /// </summary>
    public static readonly EmptyHeaders Instance = new EmptyHeaders();

    EmptyHeaders()
    {
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return Enumerable.Empty<KeyValuePair<string, object>>();
    }

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        value = default;
        return false;
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
        return defaultValue;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? Get<T>(string key, T? defaultValue = null)
        where T : struct
    {
        return defaultValue;
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
