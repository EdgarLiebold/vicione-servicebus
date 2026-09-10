using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines the operations required by client context supervisor.</summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
