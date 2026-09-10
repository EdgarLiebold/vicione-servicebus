using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Defines sql host configuration.</summary>
public interface ISqlHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<ISqlReceiveEndpointConfigurator>
{
    /// <summary>Gets the connection context supervisor.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Gets or sets the settings.</summary>
    SqlHostSettings Settings { get; set; }

    /// <summary>Gets the topology.</summary>
    new ISqlBusTopology Topology { get; }

    /// <summary>Apply the endpoint definition to the receive endpoint configurator.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="definition">The definition.</param>
    void ApplyEndpointDefinition(ISqlReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receive endpoint configuration.</returns>
    ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<ISqlReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receive endpoint configuration.</returns>
    ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(SqlReceiveSettings settings,
        ISqlEndpointConfiguration endpointConfiguration, Action<ISqlReceiveEndpointConfigurator>? configure = null);
}
