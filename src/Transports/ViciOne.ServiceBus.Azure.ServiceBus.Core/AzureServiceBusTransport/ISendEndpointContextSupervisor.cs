using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface ISendEndpointContextSupervisor :
    ITransportSupervisor<SendEndpointContext>
{
}
