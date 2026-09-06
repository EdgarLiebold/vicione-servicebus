using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Builds an Azure Service Bus entity-backed receive endpoint into a host.</summary>
public interface IServiceBusEntityEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IServiceBusEndpointConfiguration
{
    /// <summary>Creates the receive transport and adds the configured endpoint to the host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
    void Build(IHost host);
}
