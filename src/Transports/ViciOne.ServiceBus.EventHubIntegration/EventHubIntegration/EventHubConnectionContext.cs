using System;
using System.Threading;
using Azure.Core;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.EventHubIntegration.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class EventHubConnectionContext :
    BasePipeContext,
    ConnectionContext
{
    readonly Action<EventHubProducerClientOptions>? _configureOptions;

    public EventHubConnectionContext(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _configureOptions = configureOptions;
        HostSettings = hostSettings;
        StorageSettings = storageSettings;
    }

    public IHostSettings HostSettings { get; }
    public IStorageSettings StorageSettings { get; }

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
                ?? throw new ConfigurationException("The Event Hubs namespace is not configured.");
            TokenCredential credential = HostSettings.TokenCredential
                ?? throw new ConfigurationException("The Event Hubs token credential is not configured.");
            client = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credential, options);
        }
        return client;
    }
}
