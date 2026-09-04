using System;

namespace ViciOne.ServiceBus;

public interface IAmazonSqsSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IAmazonSqsSendTopology
{
    Action<IAmazonSqsQueueConfigurator>? ConfigureErrorSettings { set; }
    Action<IAmazonSqsQueueConfigurator>? ConfigureDeadLetterSettings { set; }
}
