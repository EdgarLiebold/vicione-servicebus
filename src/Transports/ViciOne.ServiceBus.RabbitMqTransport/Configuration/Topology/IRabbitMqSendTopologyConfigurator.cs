// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IRabbitMqSendTopologyConfigurator :
        ISendTopologyConfigurator,
        IRabbitMqSendTopology
    {
        Action<IRabbitMqQueueBindingConfigurator> ConfigureErrorSettings { set; }
        Action<IRabbitMqQueueBindingConfigurator> ConfigureDeadLetterSettings { set; }
    }
}
