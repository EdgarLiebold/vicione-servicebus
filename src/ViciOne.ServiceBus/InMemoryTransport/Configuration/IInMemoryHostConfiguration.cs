using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Owns host identity, capacity, topology, and receive endpoints for one in-memory bus.</summary>
internal interface IInMemoryHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Sets the validated absolute loopback host address.</summary>
    Uri BaseAddress { set; }

    /// <summary>Gets the host settings exposed to application configuration.</summary>
    IInMemoryHostConfigurator Configurator { get; }

    /// <summary>Gets the recyclable provider that owns the message fabric.</summary>
    IInMemoryTransportProvider TransportProvider { get; }

    /// <summary>Gets the configured per-queue and delayed-delivery capacity.</summary>
    int QueueCapacity { get; }

    /// <summary>Gets the in-memory bus topology.</summary>
    new IInMemoryBusTopology Topology { get; }

    /// <summary>Applies common endpoint-definition settings to an in-memory endpoint.</summary>
    /// <param name="configurator">The endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition to apply.</param>
    void ApplyEndpointDefinition(IInMemoryReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates and registers a receive endpoint with a new endpoint configuration.</summary>
    /// <param name="queueName">The non-empty queue name.</param>
    /// <param name="configure">An optional callback applied before registration.</param>
    /// <returns>The registered receive endpoint configuration.</returns>
    IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IInMemoryReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates and registers a receive endpoint from an existing endpoint configuration.</summary>
    /// <param name="queueName">The non-empty queue name.</param>
    /// <param name="endpointConfiguration">The inherited endpoint pipeline and topology configuration.</param>
    /// <param name="configure">An optional callback applied before registration.</param>
    /// <returns>The registered receive endpoint configuration.</returns>
    IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, IInMemoryEndpointConfiguration endpointConfiguration,
        Action<IInMemoryReceiveEndpointConfigurator>? configure = null);
}
