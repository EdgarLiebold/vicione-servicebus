using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Builds one configured in-memory receive endpoint.</summary>
internal interface IInMemoryReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IInMemoryEndpointConfiguration
{
    /// <summary>Gets the transport-specific endpoint configurator.</summary>
    IInMemoryReceiveEndpointConfigurator Configurator { get; }

    /// <summary>Builds and registers the receive endpoint with a host.</summary>
    /// <param name="host">The host that owns the endpoint lifecycle.</param>
    void Build(IHost host);
}
