using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus entity endpoint configuration.
/// </summary>
public interface IServiceBusEntityEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IServiceBusEndpointConfiguration
{
    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    void Build(IHost host);
}
