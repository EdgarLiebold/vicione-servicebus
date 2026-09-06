using System;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Supervises the shared Event Hubs connection context.</summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    /// <summary>Creates a connection supervisor from namespace, storage, and producer client settings.</summary>
    /// <param name="hostSettings">The Event Hubs namespace authentication settings.</param>
    /// <param name="storageSettings">The Blob Storage checkpoint settings carried by the connection context.</param>
    /// <param name="configureOptions">The optional producer client options callback.</param>
    public ConnectionContextSupervisor(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions)
        : base(new ConnectionContextFactory(hostSettings, storageSettings, configureOptions))
    {
    }
}
