using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Defines ActiveMQ host settings, topology, connection supervision, and endpoint creation.</summary>
public interface IActiveMqHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IActiveMqReceiveEndpointConfigurator>
{
    /// <summary>Gets or sets the required ActiveMQ broker settings.</summary>
    ActiveMqHostSettings Settings { get; set; }

    /// <summary>Gets or sets whether ActiveMQ Artemis compatibility is enabled.</summary>
    bool IsArtemis { get; set; }

    /// <summary>Gets the broker connection supervisor.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Gets the ActiveMQ bus topology.</summary>
    new IActiveMqBusTopology Topology { get; }

    /// <summary>Applies an endpoint definition, including temporary-queue semantics.</summary>
    /// <param name="configurator">The endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition to apply.</param>
    void ApplyEndpointDefinition(IActiveMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates and registers a receive-endpoint configuration for a queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional callback that configures the ActiveMQ endpoint.</param>
    /// <returns>The registered receive-endpoint configuration.</returns>
    IActiveMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IActiveMqReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates and registers a receive endpoint from explicit queue and endpoint settings.</summary>
    /// <param name="settings">The queue receive settings.</param>
    /// <param name="endpointConfiguration">The endpoint's shared configuration.</param>
    /// <param name="configure">An optional callback that configures the ActiveMQ endpoint.</param>
    /// <returns>The registered receive-endpoint configuration.</returns>
    IActiveMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(ActiveMqQueueReceiveSettings settings,
        IActiveMqEndpointConfiguration endpointConfiguration, Action<IActiveMqReceiveEndpointConfigurator>? configure = null);
}
