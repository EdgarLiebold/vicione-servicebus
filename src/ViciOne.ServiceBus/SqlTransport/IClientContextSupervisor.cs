using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for client context supervisor.
/// </summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
