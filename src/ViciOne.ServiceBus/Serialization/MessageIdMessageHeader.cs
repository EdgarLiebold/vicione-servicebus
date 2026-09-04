using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a message id message header implementation.
/// </summary>
public class MessageIdMessageHeader :
    Headers
{
    readonly Guid _messageId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageId">The message id value.</param>
    public MessageIdMessageHeader(Guid messageId)
    {
        _messageId = messageId;
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        yield return new KeyValuePair<string, object>(nameof(MessageContext.MessageId), _messageId);
    }

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (key.Equals(nameof(MessageContext.MessageId)))
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
        if (key.Equals(nameof(MessageContext.MessageId)))
            return _messageId as T;

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
        if (key.Equals(nameof(MessageContext.MessageId)))
        {
            return _messageId is T result
                ? result
                : default;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        yield return new HeaderValue(nameof(MessageContext.MessageId), _messageId);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
