using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Apache.NMS;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqHeaderProvider :
    IHeaderProvider
{
    readonly IMessage _message;

    public ActiveMqHeaderProvider(IMessage message)
    {
        _message = message;
    }

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
            if (_message.NMSTimestamp > DateTimeConstants.Epoch.UtcDateTime)
            {
                value = new DateTimeOffset(DateTime.SpecifyKind(_message.NMSTimestamp, DateTimeKind.Utc));
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
