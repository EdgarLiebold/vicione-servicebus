using System;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.EventHubIntegration.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    public ConnectionContextSupervisor(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions>? configureOptions)
        : base(new ConnectionContextFactory(hostSettings, storageSettings, configureOptions))
    {
    }
}
