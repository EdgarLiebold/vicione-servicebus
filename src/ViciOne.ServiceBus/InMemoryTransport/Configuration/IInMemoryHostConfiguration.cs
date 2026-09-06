using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Defines in memory host configuration.</summary>
public interface IInMemoryHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Set the host's base address.</summary>
    Uri BaseAddress { set; }

    /// <summary>Gets the configurator.</summary>
    IInMemoryHostConfigurator Configurator { get; }

    /// <summary>Gets the transport provider.</summary>
    IInMemoryTransportProvider TransportProvider { get; }

    /// <summary>Gets the queue capacity.</summary>
    int QueueCapacity { get; }

    /// <summary>Gets the topology.</summary>
    new IInMemoryBusTopology Topology { get; }

    /// <summary>Applies endpoint definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="definition">The definition.</param>
    void ApplyEndpointDefinition(IInMemoryReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receive endpoint configuration.</returns>
    IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IInMemoryReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receive endpoint configuration.</returns>
    IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, IInMemoryEndpointConfiguration endpointConfiguration,
        Action<IInMemoryReceiveEndpointConfigurator>? configure = null);
}
