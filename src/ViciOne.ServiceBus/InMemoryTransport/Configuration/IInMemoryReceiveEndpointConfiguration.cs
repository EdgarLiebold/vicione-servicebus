using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Defines the contract for in memory receive endpoint configuration.
/// </summary>
public interface IInMemoryReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IInMemoryEndpointConfiguration
{
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    IInMemoryReceiveEndpointConfigurator Configurator { get; }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    void Build(IHost host);
}
