using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
