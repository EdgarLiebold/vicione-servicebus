using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Projects Azure Service Bus system and application properties as transport headers.</summary>
public class ServiceBusHeaderProvider :
    IHeaderProvider
{
    readonly ServiceBusReceivedMessage _message;

    /// <summary>Initializes the provider for a received Azure Service Bus message.</summary>
    /// <param name="message">The received message whose headers are exposed.</param>
    public ServiceBusHeaderProvider(ServiceBusReceivedMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>Enumerates present system headers and application properties other than the broker-owned sent time.</summary>
    /// <returns>The transport header sequence.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        if (!string.IsNullOrWhiteSpace(_message.MessageId))
            yield return new KeyValuePair<string, object>(MessageHeaders.MessageId, _message.MessageId);
        if (!string.IsNullOrWhiteSpace(_message.CorrelationId))
            yield return new KeyValuePair<string, object>(nameof(_message.CorrelationId), _message.CorrelationId);
        if (!string.IsNullOrWhiteSpace(_message.ContentType))
            yield return new KeyValuePair<string, object>(MessageHeaders.ContentType, _message.ContentType);

        if (_message.ApplicationProperties != null)
        {
            foreach (KeyValuePair<string, object> header in _message.ApplicationProperties)
            {
                if (!MessageHeaders.TransportSentTime.Equals(header.Key, StringComparison.OrdinalIgnoreCase))
                    yield return header;
            }
        }
    }

    /// <summary>Gets an application or mapped Azure Service Bus system property by header name.</summary>
    /// <param name="key">The exact application-property name or a case-insensitive mapped system-property name.</param>
    /// <param name="value">The header value when present.</param>
    /// <returns><see langword="true"/> when a non-null mapped value is available.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (MessageHeaders.TransportSentTime.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.EnqueuedTime.UtcDateTime;
            return true;
        }

        if (_message.ApplicationProperties != null)
        {
            if (_message.ApplicationProperties.TryGetValue(key, out value) && value != null)
                return true;

            if (DiagnosticPropagationHeaders.ActivityId.Equals(key, StringComparison.OrdinalIgnoreCase)
                && _message.ApplicationProperties.TryGetValue(DiagnosticPropagationHeaders.AzureDiagnosticId, out value)
                && value != null)
                return true;
        }

        if (nameof(_message.MessageId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.MessageId;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (nameof(_message.CorrelationId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.CorrelationId;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (MessageHeaders.ContentType.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.ContentType;
            return value != null;
        }

        value = default;
        return false;
    }
}
