using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a move transport headers implementation.
/// </summary>
public class MoveTransportHeaders :
    SendHeaders
{
    readonly IBasicProperties _basicProperties;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="basicProperties">The basic properties value.</param>
    public MoveTransportHeaders(IBasicProperties basicProperties)
    {
        _basicProperties = basicProperties;
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

        _basicProperties.Headers ??= new Dictionary<string, object?>();

        if (value == null)
            _basicProperties.Headers.Remove(key);
        else
            _basicProperties.Headers[key] = value;
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

        _basicProperties.Headers ??= new Dictionary<string, object?>();

        if (overwrite)
        {
            if (value == null)
                _basicProperties.Headers.Remove(key);
            else
                _basicProperties.Headers[key] = value;
        }
        else if (value != null && !_basicProperties.Headers.ContainsKey(key))
            _basicProperties.Headers.Add(key, value);
    }

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (_basicProperties.Headers == null)
        {
            value = null;
            return false;
        }

        var found = _basicProperties.Headers.TryGetValue(key, out value);
        if (found && value != null)
        {
            if (value is byte[] bytes)
                value = Encoding.UTF8.GetString(bytes);

            return true;
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
        return _basicProperties.IsHeadersPresent() && _basicProperties.Headers != null
            ? _basicProperties.Headers.Where(x => x.Value != null).Select(x => new KeyValuePair<string, object>(x.Key, x.Value!))
            : Enumerable.Empty<KeyValuePair<string, object>>();
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
        throw new NotSupportedException("RabbitMQ move-transport headers do not support object-based retrieval.");
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
        throw new NotSupportedException("RabbitMQ move-transport headers do not support object-based retrieval.");
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return _basicProperties.IsHeadersPresent() && _basicProperties.Headers != null
            ? _basicProperties.Headers.Where(x => x.Value != null)
                .Select(x => new HeaderValue(new KeyValuePair<string, object>(x.Key, x.Value!))).GetEnumerator()
            : Enumerable.Empty<HeaderValue>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
