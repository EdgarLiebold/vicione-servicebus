using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq send topology configurator.
/// </summary>
public interface IActiveMqSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IActiveMqSendTopology
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    Action<IActiveMqQueueConfigurator> ConfigureErrorSettings { set; }

    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    Action<IActiveMqQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
