// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    using Agents;
    using Transports;


    public class SendEndpointContextSupervisor :
        TransportPipeContextSupervisor<SendEndpointContext>,
        ISendEndpointContextSupervisor
    {
        public SendEndpointContextSupervisor(IPipeContextFactory<SendEndpointContext> contextFactory)
            : base(contextFactory)
        {
        }
    }
}
