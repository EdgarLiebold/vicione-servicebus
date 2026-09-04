using System;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a connection context supervisor implementation.
/// </summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostSettings">The host settings value.</param>
    /// <param name="storageSettings">The storage settings value.</param>
    /// <param name="configureOptions">The configure options value.</param>
    public ConnectionContextSupervisor(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions)
        : base(new ConnectionContextFactory(hostSettings, storageSettings, configureOptions))
    {
    }
}
