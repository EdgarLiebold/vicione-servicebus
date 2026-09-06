using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Defines in memory receive endpoint configuration.</summary>
public interface IInMemoryReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IInMemoryEndpointConfiguration
{
    /// <summary>Gets the configurator.</summary>
    IInMemoryReceiveEndpointConfigurator Configurator { get; }

    /// <summary>Builds the configured component.</summary>
    /// <param name="host">The host.</param>
    void Build(IHost host);
}
