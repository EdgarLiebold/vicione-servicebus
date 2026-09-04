using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql send topology configurator.
/// </summary>
public interface ISqlSendTopologyConfigurator :
    ISendTopologyConfigurator,
    ISqlSendTopology
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    Action<ISqlQueueConfigurator> ConfigureErrorSettings { set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    Action<ISqlQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
