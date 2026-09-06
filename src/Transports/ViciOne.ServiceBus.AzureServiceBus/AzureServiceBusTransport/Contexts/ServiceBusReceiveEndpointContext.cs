using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes Azure Service Bus topology and processor supervision from a receive endpoint.</summary>
public interface ServiceBusReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the broker entities deployed for the endpoint.</summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>Gets the supervisor that owns the endpoint's processor client.</summary>
    IClientContextSupervisor ClientContextSupervisor { get; }
}
