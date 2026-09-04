using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class SendEndpointContextSupervisor :
    TransportPipeContextSupervisor<SendEndpointContext>,
    ISendEndpointContextSupervisor
{
    public SendEndpointContextSupervisor(IPipeContextFactory<SendEndpointContext> contextFactory)
        : base(contextFactory)
    {
    }
}
