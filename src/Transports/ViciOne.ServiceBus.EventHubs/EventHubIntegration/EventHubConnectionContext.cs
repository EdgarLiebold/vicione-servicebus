using System;
using System.Threading;
using Azure.Core;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub connection context implementation.
/// </summary>
public class EventHubConnectionContext :
    BasePipeContext,
    ConnectionContext
{
    readonly Action<EventHubProducerClientOptions>? _configureOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostSettings">The host settings value.</param>
    /// <param name="storageSettings">The storage settings value.</param>
    /// <param name="configureOptions">The configure options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public EventHubConnectionContext(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _configureOptions = configureOptions;
        HostSettings = hostSettings;
        StorageSettings = storageSettings;
    }

    /// <summary>
    /// Gets the host settings value.
    /// </summary>
    public IHostSettings HostSettings { get; }
    /// <summary>
    /// Gets the storage settings value.
    /// </summary>
    public IStorageSettings StorageSettings { get; }

    /// <summary>
    /// Creates event hub client.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <returns>The result of the operation.</returns>
    public EventHubProducerClient CreateEventHubClient(string eventHubName)
    {
        var options = new EventHubProducerClientOptions();
        _configureOptions?.Invoke(options);
        EventHubProducerClient client;
        if (!string.IsNullOrWhiteSpace(HostSettings.ConnectionString))
            client = new EventHubProducerClient(HostSettings.ConnectionString, eventHubName, options);
        else
        {
            string fullyQualifiedNamespace = HostSettings.FullyQualifiedNamespace
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Event Hub Connection Context", "unknown", "The Event Hubs namespace is not configured.", "Correct the named configuration before starting the host"));
            TokenCredential credential = HostSettings.TokenCredential
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Event Hub Connection Context", "unknown", "The Event Hubs token credential is not configured.", "Correct the named configuration before starting the host"));
            client = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credential, options);
        }
        return client;
    }
}
