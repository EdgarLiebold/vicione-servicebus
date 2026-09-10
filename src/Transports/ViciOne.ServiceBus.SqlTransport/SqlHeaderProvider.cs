using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Projects persisted SQL message metadata through the receive-header abstraction.</summary>
public class SqlHeaderProvider :
    IHeaderProvider
{
    readonly SqlTransportMessage _message;

    /// <summary>Creates a header provider for one materialized SQL transport message.</summary>
    /// <param name="message">The transport message whose metadata is exposed.</param>
    public SqlHeaderProvider(SqlTransportMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>Enumerates application and transport headers once, with transport-owned values taking precedence.</summary>
    /// <returns>The merged case-insensitive header set.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        var headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> header in _message.GetHeaders().GetAll())
            headers[header.Key] = header.Value;
        foreach (KeyValuePair<string, object> header in _message.GetTransportHeaders().GetAll())
            headers[header.Key] = header.Value;

        return headers;
    }

    /// <summary>Attempts to get header.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        switch (key)
        {
            case MessageHeaders.ContentType:
                value = _message.ContentType;
                return value != null;
            case MessageHeaders.MessageType:
                value = _message.MessageType;
                return value != null;
            case MessageHeaders.MessageId:
                value = _message.MessageId;
                return value != null;
            case MessageHeaders.CorrelationId:
                value = _message.CorrelationId;
                return value != null;
            case MessageHeaders.ConversationId:
                value = _message.ConversationId;
                return value != null;
            case MessageHeaders.RequestId:
                value = _message.RequestId;
                return value != null;
            case MessageHeaders.InitiatorId:
                value = _message.InitiatorId;
                return value != null;
            case MessageHeaders.SourceAddress:
                value = _message.SourceAddress;
                return value != null;
            case MessageHeaders.ResponseAddress:
                value = _message.ResponseAddress;
                return value != null;
            case MessageHeaders.FaultAddress:
                value = _message.FaultAddress;
                return value != null;
            case MessageHeaders.TransportMessageId:
                value = _message.TransportMessageId;
                return true;
            case nameof(_message.MessageDeliveryId):
                value = _message.MessageDeliveryId;
                return true;
            case nameof(_message.DeliveryCount):
                value = _message.DeliveryCount;
                return true;
            case MessageHeaders.RedeliveryCount:
                int? redeliveryCount = _message.GetTransportHeaders()
                    .Get(MessageHeaders.RedeliveryCount, default(int?));
                if (redeliveryCount.HasValue)
                {
                    value = redeliveryCount.Value;
                    return true;
                }

                if (_message.DeliveryCount > 1)
                {
                    value = _message.DeliveryCount - 1;
                    return true;
                }

                value = default;
                return false;
            case nameof(_message.RoutingKey):
                value = _message.RoutingKey;
                return value != null;
            case nameof(_message.PartitionKey):
                value = _message.PartitionKey;
                return value != null;
        }

        if (_message.GetTransportHeaders().TryGetHeader(key, out value))
            return true;

        if (_message.GetHeaders().TryGetHeader(key, out value))
            return true;

        value = default;
        return false;
    }
}
