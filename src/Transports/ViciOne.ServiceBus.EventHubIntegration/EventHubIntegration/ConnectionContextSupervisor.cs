// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System;
    using Azure.Messaging.EventHubs.Producer;
    using Configuration;
    using Transports;


    public class ConnectionContextSupervisor :
        TransportPipeContextSupervisor<ConnectionContext>,
        IConnectionContextSupervisor
    {
        public ConnectionContextSupervisor(IHostSettings hostSettings, IStorageSettings storageSettings, Action<EventHubProducerClientOptions> configureOptions)
            : base(new ConnectionContextFactory(hostSettings, storageSettings, configureOptions))
        {
        }
    }
}
