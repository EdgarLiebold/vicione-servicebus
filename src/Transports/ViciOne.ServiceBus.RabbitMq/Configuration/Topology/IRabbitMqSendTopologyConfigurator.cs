using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq send topology configurator.
/// </summary>
public interface IRabbitMqSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IRabbitMqSendTopology
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    Action<IRabbitMqQueueBindingConfigurator>? ConfigureErrorSettings { set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    Action<IRabbitMqQueueBindingConfigurator>? ConfigureDeadLetterSettings { set; }
}
