using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Defines the contract for in memory host configuration.
/// </summary>
public interface IInMemoryHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>
    /// Set the host's base address
    /// </summary>
    Uri BaseAddress { set; }

    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    IInMemoryHostConfigurator Configurator { get; }

    /// <summary>
    /// Gets the transport provider value.
    /// </summary>
    IInMemoryTransportProvider TransportProvider { get; }

    /// <summary>
    /// Gets the queue capacity value.
    /// </summary>
    int QueueCapacity { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IInMemoryBusTopology Topology { get; }

    /// <summary>
    /// Performs the apply endpoint definition operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="definition">The definition value.</param>
    void ApplyEndpointDefinition(IInMemoryReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IInMemoryReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, IInMemoryEndpointConfiguration endpointConfiguration,
        Action<IInMemoryReceiveEndpointConfigurator>? configure = null);
}
