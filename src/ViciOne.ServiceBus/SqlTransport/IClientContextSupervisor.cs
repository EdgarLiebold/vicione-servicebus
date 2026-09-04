using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
