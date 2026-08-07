// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System;


public interface IAmazonSqsSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IAmazonSqsSendTopology
{
    Action<IAmazonSqsQueueConfigurator>? ConfigureErrorSettings { set; }
    Action<IAmazonSqsQueueConfigurator>? ConfigureDeadLetterSettings { set; }
}
