using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub header provider implementation.
/// </summary>
public class EventHubHeaderProvider :
    IHeaderProvider
{
    readonly EventData _eventData;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="eventData">The event data value.</param>
    public EventHubHeaderProvider(EventData eventData)
    {
        _eventData = eventData;
    }

    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        if (!string.IsNullOrWhiteSpace(_eventData.MessageId))
            yield return new KeyValuePair<string, object>(nameof(MessageContext.MessageId), _eventData.MessageId);
        if (!string.IsNullOrWhiteSpace(_eventData.CorrelationId))
            yield return new KeyValuePair<string, object>(nameof(MessageContext.CorrelationId), _eventData.CorrelationId);

        foreach (var key in _eventData.Properties.Keys)
        {
            var value = _eventData.Properties[key];

            if (value != null)
                yield return new KeyValuePair<string, object>(key, value);
        }
    }

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (nameof(MessageContext.MessageId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _eventData.MessageId;
            return value != null;
        }

        if (nameof(MessageContext.CorrelationId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _eventData.CorrelationId;
            return value != null;
        }

        var found = _eventData.Properties.TryGetValue(key, out value);
        if (found && value != null)
            return true;

        value = null;
        return false;
    }
}
