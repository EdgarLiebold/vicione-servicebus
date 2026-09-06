using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ send topology and generated fault-queue settings.</summary>
public interface IRabbitMqSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IRabbitMqSendTopology
{
    /// <summary>Sets the callback that customizes generated error-queue topology.</summary>
    Action<IRabbitMqQueueBindingConfigurator>? ConfigureErrorSettings { set; }
    /// <summary>Sets the callback that customizes generated dead-letter-queue topology.</summary>
    Action<IRabbitMqQueueBindingConfigurator>? ConfigureDeadLetterSettings { set; }
}
