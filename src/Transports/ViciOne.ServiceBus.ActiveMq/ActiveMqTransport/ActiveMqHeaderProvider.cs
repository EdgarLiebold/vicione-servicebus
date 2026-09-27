using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Apache.NMS;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Reads framework and native properties from an Apache NMS message.</summary>
public class ActiveMqHeaderProvider :
    IHeaderProvider
{
    readonly IMessage _message;

    /// <summary>Creates a header provider for an Apache NMS message.</summary>
    /// <param name="message">The native message whose headers are exposed.</param>
    public ActiveMqHeaderProvider(IMessage message)
    {
        _message = message;
    }

    /// <summary>Enumerates the transport message identifier, correlation identifier, and native message properties.</summary>
    /// <returns>The available header name/value pairs.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        if (_message.NMSMessageId is { } messageId)
            yield return new KeyValuePair<string, object>(MessageHeaders.TransportMessageId, messageId);
        if (_message.NMSCorrelationID is { } correlationId)
            yield return new KeyValuePair<string, object>(nameof(MessageContext.CorrelationId), correlationId);

        foreach (string key in _message.Properties.Keys)
        {
            var value = _message.Properties[key];

            if (value != null)
                yield return new KeyValuePair<string, object>(key, value);
        }
    }

    /// <summary>Tries to read a framework header or native message property.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value, when present and non-null.</param>
    /// <returns><see langword="true" /> when a non-null value is available; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (MessageHeaders.TransportMessageId.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.NMSMessageId;
            return value != null;
        }

        if (nameof(MessageContext.CorrelationId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.NMSCorrelationID;
            return value != null;
        }

        if (MessageHeaders.TransportSentTime.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            DateTime timestamp = _message.NMSTimestamp;
            DateTime utcTimestamp = timestamp.Kind == DateTimeKind.Local
                ? timestamp.ToUniversalTime()
                : DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
            if (utcTimestamp > DateTimeConstants.Epoch.UtcDateTime)
            {
                value = new DateTimeOffset(utcTimestamp);
                return true;
            }
        }

        var found = _message.Properties.Contains(key);
        if (found)
        {
            value = _message.Properties[key];
            return value != null;
        }

        value = null;
        return false;
    }
}
