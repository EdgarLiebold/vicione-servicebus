using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for client context supervisor.
/// </summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
