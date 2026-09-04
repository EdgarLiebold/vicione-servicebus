using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus receive endpoint context.
/// </summary>
public interface ServiceBusReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the client context supervisor value.
    /// </summary>
    IClientContextSupervisor ClientContextSupervisor { get; }
}
