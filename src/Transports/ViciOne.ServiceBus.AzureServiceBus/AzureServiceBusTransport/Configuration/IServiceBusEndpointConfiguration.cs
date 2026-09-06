using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Exposes Azure Service Bus topology settings for an endpoint configuration.</summary>
public interface IServiceBusEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the endpoint's transport-specific topology configuration.</summary>
    new IServiceBusTopologyConfiguration Topology { get; }
}
