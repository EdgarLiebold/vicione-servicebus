using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Adapts RabbitMQ AMQP basic-property headers to the send-header contract used by message moves.</summary>
public class MoveTransportHeaders :
    SendHeaders
{
    readonly IBasicProperties _basicProperties;

    /// <summary>Creates a header adapter over mutable AMQP basic properties.</summary>
    /// <param name="basicProperties">The properties whose header table is read and updated.</param>
    public MoveTransportHeaders(IBasicProperties basicProperties)
    {
        _basicProperties = basicProperties;
    }

    /// <summary>Sets a string header, or removes it when the value is <see langword="null" />.</summary>
    /// <param name="key">The AMQP header key.</param>
    /// <param name="value">The string header value.</param>
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

    /// <summary>Sets or conditionally adds an AMQP header.</summary>
    /// <param name="key">The AMQP header key.</param>
    /// <param name="value">The header value; <see langword="null" /> removes an overwritten value.</param>
    /// <param name="overwrite">Whether an existing value may be replaced or removed.</param>
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

    /// <summary>Reads a non-null AMQP header and decodes byte arrays as UTF-8 text.</summary>
    /// <param name="key">The AMQP header key.</param>
    /// <param name="value">The decoded header value when present.</param>
    /// <returns><see langword="true" /> when a non-null header exists.</returns>
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

    /// <summary>Enumerates all non-null AMQP headers as key/value pairs.</summary>
    /// <returns>The current non-null headers.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _basicProperties.IsHeadersPresent() && _basicProperties.Headers != null
            ? _basicProperties.Headers.Where(x => x.Value != null).Select(x => new KeyValuePair<string, object>(x.Key, x.Value!))
            : Enumerable.Empty<KeyValuePair<string, object>>();
    }

    /// <summary>Throws because typed object retrieval is not supported by this move-header adapter.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="key">The AMQP header name.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The requested value.</returns>
    public T Get<T>(string key, T? defaultValue)
        where T : class
    {
        throw new NotSupportedException("RabbitMQ move-transport headers do not support object-based retrieval.");
    }

    /// <summary>Throws because typed object retrieval is not supported by this move-header adapter.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="key">The AMQP header name.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The requested value.</returns>
    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        throw new NotSupportedException("RabbitMQ move-transport headers do not support object-based retrieval.");
    }

    /// <summary>Enumerates all non-null AMQP headers through the send-header abstraction.</summary>
    /// <returns>An enumerator over the current headers.</returns>
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
