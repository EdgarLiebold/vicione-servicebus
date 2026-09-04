using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class MoveTransportHeaders :
    SendHeaders
{
    readonly IBasicProperties _basicProperties;

    public MoveTransportHeaders(IBasicProperties basicProperties)
    {
        _basicProperties = basicProperties;
    }

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

    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _basicProperties.IsHeadersPresent() && _basicProperties.Headers != null
            ? _basicProperties.Headers.Where(x => x.Value != null).Select(x => new KeyValuePair<string, object>(x.Key, x.Value!))
            : Enumerable.Empty<KeyValuePair<string, object>>();
    }

    public T Get<T>(string key, T? defaultValue)
        where T : class
    {
        throw new NotImplementedByDesignException("Move transport does not support object-based header retrieval");
    }

    public T? Get<T>(string key, T? defaultValue)
        where T : struct
    {
        throw new NotImplementedByDesignException("Move transport does not support object-based header retrieval");
    }

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
