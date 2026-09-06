using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Exposes Event Hubs message identifiers and application properties as service-bus headers.</summary>
public class EventHubHeaderProvider :
    IHeaderProvider
{
    readonly EventData _eventData;

    /// <summary>Creates a header provider for one Azure SDK event.</summary>
    /// <param name="eventData">The event whose identifiers and properties are exposed.</param>
    public EventHubHeaderProvider(EventData eventData)
    {
        _eventData = eventData;
    }

    /// <summary>Enumerates non-null message identifiers and application properties.</summary>
    /// <returns>The headers available on the event.</returns>
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

    /// <summary>Attempts to read a message identifier or application property by name.</summary>
    /// <param name="key">The case-insensitive identifier name or exact application-property key.</param>
    /// <param name="value">Receives the non-null header value when found.</param>
    /// <returns><see langword="true" /> when a non-null value exists; otherwise, <see langword="false" />.</returns>
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
