using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
}
