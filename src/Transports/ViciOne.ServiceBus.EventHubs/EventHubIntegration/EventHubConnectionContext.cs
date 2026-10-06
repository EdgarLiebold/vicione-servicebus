using System;
using System.Threading;
using Azure.Core;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Creates Event Hubs producer clients from shared namespace authentication and client options.</summary>
public class EventHubConnectionContext :
    BasePipeContext,
    ConnectionContext
{
    readonly Action<EventHubProducerClientOptions>? _configureOptions;

    /// <summary>Creates a connection context from the supplied rider settings.</summary>
    /// <param name="hostSettings">The Event Hubs namespace authentication settings.</param>
    /// <param name="storageSettings">The Blob Storage checkpoint settings exposed by this context.</param>
    /// <param name="configureOptions">The optional producer client options callback.</param>
    /// <param name="cancellationToken">Stops operations using this connection context.</param>
    public EventHubConnectionContext(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _configureOptions = configureOptions;
        HostSettings = hostSettings;
        StorageSettings = storageSettings;
    }

    /// <summary>Gets the Event Hubs namespace authentication settings.</summary>
    public IHostSettings HostSettings { get; }
    /// <summary>Gets the Blob Storage checkpoint settings.</summary>
    public IStorageSettings StorageSettings { get; }

    /// <summary>Creates an Azure SDK producer client using the configured authentication mode.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <returns>The configured producer client.</returns>
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
