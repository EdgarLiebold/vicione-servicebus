using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ClientContextSupervisor :
    TransportPipeContextSupervisor<ClientContext>,
    IClientContextSupervisor
{
    public ClientContextSupervisor(IPipeContextFactory<ClientContext> contextFactory)
        : base(contextFactory)
    {
    }
}
