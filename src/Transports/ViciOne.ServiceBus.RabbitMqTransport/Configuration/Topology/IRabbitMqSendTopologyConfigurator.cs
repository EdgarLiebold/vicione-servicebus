using System;

namespace ViciOne.ServiceBus;

public interface IRabbitMqSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IRabbitMqSendTopology
{
    Action<IRabbitMqQueueBindingConfigurator> ConfigureErrorSettings { set; }
    Action<IRabbitMqQueueBindingConfigurator> ConfigureDeadLetterSettings { set; }
}
