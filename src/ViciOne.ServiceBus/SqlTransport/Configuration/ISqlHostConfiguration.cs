using System;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Defines the contract for sql host configuration.
/// </summary>
public interface ISqlHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<ISqlReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    SqlHostSettings Settings { get; set; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new ISqlBusTopology Topology { get; }

    /// <summary>
    /// Apply the endpoint definition to the receive endpoint configurator
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="definition"></param>
    void ApplyEndpointDefinition(ISqlReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<ISqlReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(SqlReceiveSettings settings,
        ISqlEndpointConfiguration endpointConfiguration, Action<ISqlReceiveEndpointConfigurator>? configure = null);
}
