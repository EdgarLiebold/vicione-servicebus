using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Defines the contract for active mq host configuration.
/// </summary>
public interface IActiveMqHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IActiveMqReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    ActiveMqHostSettings Settings { get; set; }

    /// <summary>
    /// Gets or sets the is artemis value.
    /// </summary>
    bool IsArtemis { get; set; }

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IActiveMqBusTopology Topology { get; }

    /// <summary>
    /// Apply the endpoint definition to the receive endpoint configurator
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="definition"></param>
    void ApplyEndpointDefinition(IActiveMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>
    /// Create a receive endpoint configuration for the default host
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    IActiveMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IActiveMqReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Create a receive endpoint configuration for the default host
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="endpointConfiguration"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    IActiveMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(ActiveMqQueueReceiveSettings settings,
        IActiveMqEndpointConfiguration endpointConfiguration, Action<IActiveMqReceiveEndpointConfigurator>? configure = null);
}
