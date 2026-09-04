using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for connection context supervisor.
/// </summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
}
