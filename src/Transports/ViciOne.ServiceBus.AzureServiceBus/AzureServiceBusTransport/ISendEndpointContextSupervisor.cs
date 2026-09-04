using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for send endpoint context supervisor.
/// </summary>
public interface ISendEndpointContextSupervisor :
    ITransportSupervisor<SendEndpointContext>
{
}
