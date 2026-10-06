using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Exposes a single message identifier through the standard header contract.</summary>
public sealed class MessageIdHeaders :
    Headers
{
    readonly Guid _messageId;

    /// <summary>Creates a header collection for the supplied message identifier.</summary>
    /// <param name="messageId">The message identifier.</param>
    public MessageIdHeaders(Guid messageId)
    {
        _messageId = messageId;
    }

    /// <summary>Enumerates the message-identifier header.</summary>
    /// <returns>The single header entry.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        yield return new KeyValuePair<string, object>(nameof(MessageContext.MessageId), _messageId);
    }

    /// <summary>Tries to obtain the message identifier.</summary>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="value">The message identifier when <paramref name="key" /> names it.</param>
    /// <returns><see langword="true" /> for the message-identifier header; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.Equals(nameof(MessageContext.MessageId), StringComparison.OrdinalIgnoreCase))
        {
            value = _messageId;
            return true;
        }

        value = default;
        return false;
    }

    T? Headers.Get<T>(string key, T? defaultValue)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.Equals(nameof(MessageContext.MessageId), StringComparison.OrdinalIgnoreCase))
            return _messageId as T ?? defaultValue;

        return defaultValue;
    }

    /// <summary>Gets the message identifier as the requested value type.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="key">The non-empty header name.</param>
    /// <param name="defaultValue">The fallback for a different header name or incompatible value type.</param>
    /// <returns>The message identifier as <typeparamref name="T" />, or the fallback.</returns>
    public T? Get<T>(string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.Equals(nameof(MessageContext.MessageId), StringComparison.OrdinalIgnoreCase))
        {
            return _messageId is T result
                ? result
                : defaultValue;
        }

        return defaultValue;
    }

    /// <summary>Enumerates the message-identifier header.</summary>
    /// <returns>An enumerator over the single header.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        yield return new HeaderValue(nameof(MessageContext.MessageId), _messageId);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
